﻿using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Core.Translate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nikse.SubtitleEdit.Core.AutoTranslate
{
    public class LibreTranslate : IAutoTranslator
    {
        private HttpClient _httpClient;

        public string Name { get; set; } = "LibreTranslate";
        public string Url => "https://github.com/LibreTranslate/LibreTranslate";

        public void Initialize()
        {
            _httpClient?.Dispose();
            _httpClient = new HttpClient(); //DownloaderFactory.MakeHttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
            _httpClient.BaseAddress = new Uri(Configuration.Settings.Tools.AutoTranslateLibreUrl);
        }

        public List<TranslationPair> GetSupportedSourceLanguages()
        {
            return ListLanguages();
        }

        public List<TranslationPair> GetSupportedTargetLanguages()
        {
            return ListLanguages();
        }

        public async Task<string> Translate(string text, string sourceLanguageCode, string targetLanguageCode)
        {
            var input = "{\"q\": \"" + Json.EncodeJsonText(text.Trim()) + "\", \"source\": \"" + sourceLanguageCode + "\", \"target\": \"" + targetLanguageCode + "\"}";
            var content = new StringContent(input, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var result = _httpClient.PostAsync("translate", content).Result;
            result.EnsureSuccessStatusCode();
            var bytes = await result.Content.ReadAsByteArrayAsync();
            var json = Encoding.UTF8.GetString(bytes).Trim();

            var parser = new SeJsonParser();
            var resultText = parser.GetFirstObject(json, "translatedText");
            if (resultText == null)
            {
                return string.Empty;
            }

            return Json.DecodeJsonText(resultText).Trim();
        }

        private static List<TranslationPair> ListLanguages()
        {
            var languageCodes = new List<string>
            {
                "ar",
                "az",
                "cs",
                "da",
                "de",
                "el",
                "en",
                "eo",
                "es",
                "fa",
                "fi",
                "fr",
                "ga",
                "he",
                "hi",
                "hu",
                "id",
                "it",
                "ja",
                "ko",
                "nl",
                "pl",
                "pt",
                "ru",
                "ru",
                "sk",
                "sv",
                "tr",
                "uk",
                "zh",
            };

            var result = new List<TranslationPair>();
            var cultures = Utilities.GetSubtitleLanguageCultures(false).ToList();
            foreach (var code in languageCodes)
            {
                var culture = cultures.FirstOrDefault(p=>p.TwoLetterISOLanguageName == code);
                if (culture != null)
                {
                    result.Add(new TranslationPair(culture.EnglishName, code));
                }
            }

            return result;
        }
    }
}
