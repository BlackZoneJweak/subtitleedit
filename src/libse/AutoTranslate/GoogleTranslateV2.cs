﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Http;
using Nikse.SubtitleEdit.Core.Translate;
using Nikse.SubtitleEdit.Core.Translate.Service;

namespace Nikse.SubtitleEdit.Core.AutoTranslate
{
    /// <summary>
    /// Google translate via Google Cloud V2 API - see https://cloud.google.com/translate/
    /// </summary>
    public class GoogleTranslateV2 : IAutoTranslator
    {
        private string _apiKey;
        private IDownloader _httpClient;

        public string Name { get; set; } = "Google Translate Cloud V2 API";
        public string Url => "https://translate.google.com/";

        public void Initialize()
        {
            _apiKey = Configuration.Settings.Tools.GoogleApiV2Key;
            _httpClient = DownloaderFactory.MakeHttpClient();
            _httpClient.BaseAddress = new Uri("https://translation.googleapis.com/language/translate/v2/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public List<TranslationPair> GetSupportedSourceLanguages()
        {
            return GoogleTranslationService.GetTranslationPairs();
        }

        public List<TranslationPair> GetSupportedTargetLanguages()
        {
            return GoogleTranslationService.GetTranslationPairs();
        }

        public Task<string> Translate(string text, string sourceLanguageCode, string targetLanguageCode)
        {
            var format = "text";
            var input = new StringBuilder();
            input.Append("q=" + Utilities.UrlEncode(text));
            var uri = $"?{input}&target={targetLanguageCode}&source={sourceLanguageCode}&format={format}&key={_apiKey}";
            string content;
            try
            {
                var result = _httpClient.PostAsync(uri, new StringContent(string.Empty)).Result;
                if ((int)result.StatusCode == 400)
                {
                    throw new TranslationException("API key invalid (or perhaps billing is not enabled)?");
                }
                if ((int)result.StatusCode == 403)
                {
                    throw new TranslationException("\"Perhaps billing is not enabled (or API key is invalid)?\"");
                }

                if (!result.IsSuccessStatusCode)
                {
                    throw new TranslationException($"An error occurred calling GT translate - status code: {result.StatusCode}");
                }

                content = result.Content.ReadAsStringAsync().Result;
            }
            catch (WebException webException)
            {
                var message = string.Empty;
                if (webException.Message.Contains("(400) Bad Request"))
                {
                    message = "API key invalid (or perhaps billing is not enabled)?";
                }
                else if (webException.Message.Contains("(403) Forbidden."))
                {
                    message = "Perhaps billing is not enabled (or API key is invalid)?";
                }
                throw new TranslationException(message, webException);
            }

            var resultList = new List<string>();
            var parser = new JsonParser();
            var x = (Dictionary<string, object>)parser.Parse(content);
            foreach (var k in x.Keys)
            {
                if (x[k] is Dictionary<string, object> v)
                {
                    foreach (var innerKey in v.Keys)
                    {
                        if (v[innerKey] is List<object> l)
                        {
                            foreach (var o2 in l)
                            {
                                if (o2 is Dictionary<string, object> v2)
                                {
                                    foreach (var innerKey2 in v2.Keys)
                                    {
                                        if (v2[innerKey2] is string translatedText)
                                        {
                                            try
                                            {
                                                translatedText = Regex.Unescape(translatedText);
                                            }
                                            catch
                                            {
                                                translatedText = translatedText.Replace("\\n", "\n");
                                            }

                                            translatedText = string.Join(Environment.NewLine, translatedText.SplitToLines());
                                            translatedText = TranslationHelper.PostTranslate(translatedText, targetLanguageCode);
                                            resultList.Add(translatedText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return Task.FromResult(string.Join(Environment.NewLine, resultList));
        }
    }
}
