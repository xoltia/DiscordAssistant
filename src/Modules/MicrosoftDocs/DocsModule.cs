﻿using Assistant.Services;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Modules.MicrosoftDocs
{
    [Group("docs")]
    public class DocsModule : ModuleBase
    {
        // TODO: allow searching by category (reference/learn/docs/all)

        private readonly HttpService _http;
        private static readonly string ApiBase = "https://docs.microsoft.com/api/search?search={0}&locale=en-us&%24filter=scopes%2Fany%28t%3A+t+eq+%27{1}%27%29&facet=category&%24skip=0&%24top=5";
        private static readonly string DocsBase = "https://docs.microsoft.com/en-us/search/?search={0}&category=All&scope={1}";

        private static readonly Dictionary<string, Color> ScopeThemes = new Dictionary<string, Color>
        {
            { ".NET", new Color(0x512bd4) },
            { "SQL",  new Color(0x243a5e) },
        };

        public DocsModule(HttpService http)
        {
            _http = http;
        }

        private async Task<Embed> GetDocsEmbed(string search, string scope)
        {
            Docs docs = await _http.GetModel<Docs>(HttpService.FormatUrl(ApiBase, search, scope));
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle(search)
                .WithAuthor("Microsoft Docs", "https://docs.microsoft.com/en-us/media/logos/logo-ms-social.png", "https://docs.microsoft.com")
                .WithDescription($"[View on website]({HttpService.FormatUrl(DocsBase, search, scope)})")
                .WithFields(docs.Results.Select(r =>
                    new EmbedFieldBuilder().WithName(r.Title).WithValue($"[{r.DisplayUrl.Content}]({r.Url})\n{r.Description}")
                ))
                .WithFooter($"{scope} Documentation")
                .WithColor(ScopeThemes.GetValueOrDefault(scope, Color.Purple));
            return embed.Build();
        }

        [Command, Priority(-1)]
        public Task Docs([Remainder]string search) =>
            DotnetDocs(search);

        [Command("dotnet"), Alias(".net")]
        public async Task DotnetDocs([Remainder]string search) =>
            await ReplyAsync(embed: await GetDocsEmbed(search, ".NET"));

        [Command("sql")]
        public async Task SqlDocs([Remainder]string search) =>
            await ReplyAsync(embed: await GetDocsEmbed(search, "SQL"));
    }
}
