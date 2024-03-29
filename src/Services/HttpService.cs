﻿using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Assistant.Services
{
    public class HttpService
    {
        private HttpClient _http = new HttpClient();

        public static string FormatUrl(string baseUrl, params string[] values) =>
            string.Format(baseUrl, values.Select(v => HttpUtility.UrlEncode(v)).ToArray());

        public async Task<T> GetModelAsync<T>(string url) =>
            JsonConvert.DeserializeObject<T>(await _http.GetStringAsync(url));
    }
}
