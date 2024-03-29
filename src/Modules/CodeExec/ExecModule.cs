﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Assistant.Modules.CodeExec
{
    [Group("execute"), Alias("exec")]
    [Summary("Execute code and view related information")]
    public class ExecModule : ModuleBase
    {
        private readonly string _snippets;
        private readonly ExecutionConfig _config;
        private readonly Random _random;

        private static readonly string AlphabeticChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly string ContainerDirectory = "/home/submitted_code";
        private static readonly List<ILanguage> Languages = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(ILanguage).IsAssignableFrom(t) && !t.IsInterface)
            .Select(t => (ILanguage)Activator.CreateInstance(t))
            .ToList();

        public ExecModule(AssistantConfig config, Random random)
        {
            _random = random;
            _snippets = config.SnippetPath ?? "submitted_code";
            _config = config.Exec ?? new ExecutionConfig
            {
                CPUs = 0.8F,
                Memory = "250m",
                Timeout = 10
            };
        }

        private static ILanguage GetLanguage(string name) =>
            Languages.FirstOrDefault(l => l.Name.ToLower() == name.ToLower() || l.Aliases.Any(a => a.ToLower() == name.ToLower()));

        private static Embed GetOutputEmbed(ILanguage lang, ProcessResult result)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle($"{lang.Name} - Run Results")
                .AddField("Exit code", result.ExitCode)
                .WithColor(result.ExitCode == 0 ? Color.Green : Color.Red);

            if (!string.IsNullOrEmpty(result.StandardOutput))
                embed.AddField("Out", $"```\n{result.StandardOutput}```");

            if (!string.IsNullOrEmpty(result.StandardError))
                embed.AddField("Error", $"```\n{result.StandardError}```");

            return embed.Build();
        }

        [Command]
        [UsageLimit(10, 2)]
        [Summary("Executes submitted code")]
        public async Task Execute([Remainder]CodeSnippet snippet)
        {
            ILanguage language = GetLanguage(snippet.Language);
            if (language == null)
            {
                EmbedBuilder response = new EmbedBuilder()
                    .WithTitle($"Unsupported language '{snippet.Language}'")
                    .WithDescription("**Supported languages:**\n");
                AppendLanguageDetails(response);
                await ReplyAsync(embed: response.Build());
                return;
            }
            try
            {
                ProcessResult result = await RunAsync(language, snippet.Code);
                await ReplyAsync(embed: GetOutputEmbed(language, result));
            }
            catch (TimeoutException)
            {
                await ReplyAsync($"{Context.User.Mention} Execution timed out");
            }
        }

        [Command("languages"), Alias("langs")]
        [Summary("View supported languages")]
        public Task ListLanguages()
        {
            EmbedBuilder response = new EmbedBuilder()
                .WithTitle("Supported languages:");
            AppendLanguageDetails(response);
            return ReplyAsync(embed: response.Build());
        }

        private void AppendLanguageDetails(EmbedBuilder embed)
        {
            foreach (ILanguage supported in Languages)
            {
                embed.Description += $"- {supported.Name}";
                if (supported.Aliases.Length > 0)
                    embed.Description += $" ({string.Join(", ", supported.Aliases)})";
                embed.Description += '\n';
            }
        }

        private async Task<ProcessResult> RunAsync(ILanguage lang, string code)
        {
            using DockerContainer container = await DockerContainer.CreateContainerAsync(lang.DockerImage, _config);
            // Because Java is dumb
            string mainClass = string.Create(8, _random, (Span<char> chars, Random random) =>
            {
                for (int i = 0; i < chars.Length; i++)
                    chars[i] = AlphabeticChars[_random.Next(AlphabeticChars.Length)];
            });
            string localDir = Path.Combine(_snippets, Context.User.Id.ToString());
            string codeFile = $"{mainClass}.{lang.Extension}";
            string codeFilePath = Path.Combine(localDir, codeFile);
  
            Directory.CreateDirectory(localDir);
            await File.WriteAllTextAsync(codeFilePath, string.Format(lang.SourceFile, code, mainClass));
            await container.CopyAsync(codeFilePath, Path.Combine(ContainerDirectory, codeFile));

            if (lang is ICompiledLanguage)
            {
                ICompiledLanguage compilable = lang as ICompiledLanguage;
                ProcessResult compileResult = await container.ExecAsync(string.Format(compilable.CompileCommand, codeFile), ContainerDirectory);
                if (compileResult.ExitCode != 0)
                    return compileResult;
            }
            return await container.ExecAsync(string.Format(lang.RunCommand, codeFile, mainClass), ContainerDirectory);
        }
    }
}
