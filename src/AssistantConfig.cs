﻿using Discord;
using Newtonsoft.Json;
using System.IO;

namespace Assistant
{
    public class LogConfig
    {
        [JsonProperty("severity")]
        public LogSeverity? Severity { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("extension")]
        public string Extension { get; set; }

        [JsonProperty("ext")]
        public string Ext
        {
            get => Extension;
            set => Extension = value;
        }
    }

    public class AssistantConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        // TODO: allow list of prefixes
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("log")]
        public LogConfig Log { get; set; }

        public static AssistantConfig FromFile(string path) =>
            JsonConvert.DeserializeObject<AssistantConfig>(File.ReadAllText(path));
    }
}
