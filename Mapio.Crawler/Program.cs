using Mapio.Crawler.Dto;
using Mapio.Dto.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace Mapio.Crawler
{
    public class Program
    {
        private const string _rootUri = "https://data.stat.gov.lv/api/v1/lv/OSP_PUB/";

        /// <summary>
        /// Predefined Uris which contain manually picked data.
        /// </summary>
        private static readonly List<(string Uri, string Version)> Uris = new List<(string, string)>
        {
            ("POP/IR/IRS/IRS030", "BEFORE_2021_ATR"),
            ("POP/IR/IRS/IRS030", "AFTER_2021_ATR"),
            //"iedz/iedzskaits/ikgad/ISG020.px",
            //"sociala/dsamaksa/isterm/DS100c.px",
            //"iedz/dzimst/IDG140.px",
            //"iedz/mirst/IMG081.px",
            //"iedz/laulibas/ikgad/ILG020.px",
            //"uzn/01_skaits/SRG010.px",
        };

        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.LatinExtendedA),
        };

        /// <summary>
        /// The next (WIP) iteration of the script, intent is for it to actually crawl through the entire API and find valid datasets.
        /// </summary>
        /// <remarks>
        /// Valid dataset - mappable data, i.e. data can be searched for all counties, before or after ATR.
        /// </remarks>
        public static async Task AltMain()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rootUri),
            };

            var baseHttpRequest = new HttpRequestMessage
            {
                RequestUri = httpClient.BaseAddress,
                Method = HttpMethod.Get,
            };
            var baseHttpResponse = await httpClient.SendAsync(baseHttpRequest);
            var blocks = JsonSerializer.Deserialize<List<Block>>(await baseHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
            foreach (var block in blocks)
            {
                await Task.Delay(2000);
                var blockHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(httpClient.BaseAddress, block.DbId),
                    Method = HttpMethod.Get,
                };
                var blockHttpResponse = await httpClient.SendAsync(blockHttpRequest);
                block.Levels = JsonSerializer.Deserialize<List<Level>>(await blockHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
                
                foreach (var level in block.Levels)
                {
                    await Recurse(level, blockHttpRequest.RequestUri, httpClient);
                }
            }

            var data = JsonSerializer.Serialize(blocks, _jsonSerializerOptions);
            System.IO.File.WriteAllText("output.json", data);
        }

        private static async Task Recurse(Level level, Uri lastUri, HttpClient httpClient)
        {
            await Task.Delay(2000);
            if (level.Type == "l")
            {
                var levelHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{lastUri.OriginalString}/{level.Id}"),
                    Method = HttpMethod.Get,
                };
                var levelHttpResponse = await httpClient.SendAsync(levelHttpRequest);
                level.Levels = JsonSerializer.Deserialize<List<Level>>(await levelHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
                foreach (var subLevel in level.Levels)
                {
                    await Recurse(subLevel, levelHttpRequest.RequestUri, httpClient);
                }
            }

            if (level.Type == "t")
            {
                var tableHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{lastUri.OriginalString}/{level.Id}"),
                    Method = HttpMethod.Get,
                };
                var tableHttpResponse = await httpClient.SendAsync(tableHttpRequest);
                level.Table = JsonSerializer.Deserialize<Table>(await tableHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
            }
        }

        /// <summary>
        /// The main version of the script, retrieves all unique data points from a predefined list of endpoints.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Switch for alt mode.
            //await AltMain();
            //return;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rootUri),
            };

            var configurationValues = new List<DataSetConfiguration>();
            foreach (var uri in Uris)
            {
                var httpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(httpClient.BaseAddress, uri.Uri),
                    Method = HttpMethod.Get,
                };

                var httpResponse = await httpClient.SendAsync(httpRequest);

                // For the purposes of not spamming CSB - "caching" the response.
                // await System.IO.File.WriteAllTextAsync("local.json", await httpResponse.Content.ReadAsStringAsync());
                // var response = JsonSerializer.Deserialize<Response>(await System.IO.File.ReadAllTextAsync("local.json"), _jsonSerializerOptions);

                var response = JsonSerializer.Deserialize<Response>(await httpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);

                var quarters = ExtractQuarters(response);
                var firstYearQuarters = ExtractFirstYearQuarters(response);
                var lastYearQuarters = ExtractLastYearQuarters(response);
                var subItems = ExtractSubItems(response);
                foreach (var item in subItems)
                {
                    configurationValues.Add(new DataSetConfiguration
                    {
                        Version = uri.Version,
                        Uri = uri.Uri,
                        QuerySections = response.Variables.Select(item => item.Code).ToList(),
                        QueryValues = GetQueryValues(response, item),
                        Years = GetYears(response),
                        Quarters = quarters,
                        IsQuarterly = quarters.Any(),
                        TextLat = item.Title,
                        TextEng = GetTranslation(item.Title),
                        FirstYearQuarters = ExtractFirstYearQuarters(response),
                        LastYearQuarters = ExtractLastYearQuarters(response),
                    });
                }

                await Task.Delay(2000); // Sleep to avoid too many requests response.
            };

            var configurationRoot = new DataSetConfigurationRoot
            {
                DataSetConfigurations = configurationValues,
            };

            await System.IO.File.WriteAllTextAsync("testResponse.json", JsonSerializer.Serialize(configurationRoot, _jsonSerializerOptions), Encoding.UTF8);
        }

        private static List<string> GetYears(Response response)
        {
            var yearVariable = response.Variables.FirstOrDefault(item => item.Code == "TIME");
            return yearVariable.Values.Where(value => long.Parse(value) >= 2009).ToList();
            //switch (yearVariable?.Code)
            //{
            //    case "Gads":
            //    case "Gads/Ceturksnis":
            //        return yearVariable.Values.Where(value => long.Parse(value.Substring(0, 4)) >= 2009).Select(value => value.Substring(0, 4)).Distinct().ToList();
            //    case null:
            //    default:
            //        return new List<string>();
            //}
        }

        private static List<string> ExtractQuarters(Response response)
        {
            var quarterVariable = response.Variables.FirstOrDefault(item => item.Code == "Gads/Ceturksnis");
            switch (quarterVariable?.Code)
            {
                case "Gads/Ceturksnis":
                    return quarterVariable.Values.Select(value => value.Substring(4, 2)).Distinct().ToList();
                default:
                    return new List<string>();
            }
        }

        private static string GetTranslation(string lat)
        {
            return lat;
        }

        private static List<string> ExtractFirstYearQuarters(Response response)
        {
            var quarters = new List<string> { "Q1", "Q2", "Q3", "Q4" };
            var yearQuarterVariable = response.Variables.FirstOrDefault(item => item.Code == "Gads/Ceturksnis");
            switch (yearQuarterVariable?.Code)
            {
                case "Gads/Ceturksnis":
                    return quarters.Where(q => quarters.IndexOf(q) >= quarters.IndexOf(yearQuarterVariable.Values.First().Substring(4, 2))).ToList();
                default:
                    return null;
            }
        }

        private static List<string> ExtractLastYearQuarters(Response response)
        {
            var quarters = new List<string> { "Q1", "Q2", "Q3", "Q4" };
            var yearQuarterVariable = response.Variables.FirstOrDefault(item => item.Code == "Gads/Ceturksnis");
            switch (yearQuarterVariable?.Code)
            {
                case "Gads/Ceturksnis":
                    return quarters.Where(q => quarters.IndexOf(q) <= quarters.IndexOf(yearQuarterVariable.Values.Last().Substring(4, 2))).ToList();
                default:
                    return null;
            }
        }

        private static List<(string Title, string Code)> ExtractSubItems(Response response)
        {
            var titleVariable = response.Variables.FirstOrDefault(item => item.Code == "INDICATOR" || item.Code == "Tirgus sektora un ārpus tirgus sektora uzņēmumi" || item.Code == "Sektors");
            var secondaryTitle = response.Variables.FirstOrDefault(item => item.Code == "Bruto/ Neto");
            if (titleVariable is null)
            {
                return new List<(string Title, string Code)>
                {
                    (response.Title, string.Empty),
                };
            }

            var result = new List<(string, string)>();
            for (int i = 0; i < titleVariable.Values.Count; i++)
            {
                if (secondaryTitle != null)
                {
                    for (int j = 0; j < secondaryTitle.Values.Count; j++)
                    {
                        result.Add(($"{titleVariable.ValueTexts[i]} {secondaryTitle.ValueTexts[j]}", $"{titleVariable.Values[i]}#{secondaryTitle.Values[j]}"));
                    }
                }
                else
                {
                    result.Add((titleVariable.ValueTexts[i], titleVariable.Values[i]));
                }
            }
            return result;
        }

        private static List<List<string>> GetQueryValues(Response response, (string Title, string Code) subItem)
        {
            var result = new List<List<string>>();
            foreach (var variable in response.Variables)
            {
                var entry = new List<string>();
                if (variable.Code == "INDICATOR")
                {
                    entry.Add(subItem.Code);
                }
                else if (variable.Code == "AREA")
                {
                    entry.Add("AREA");
                }
                else if (variable.Code == "TIME")
                {
                    entry.Add("TIME");
                }
                else
                {
                    entry.AddRange(variable.Values);
                }

                //if (variable.Code == "Gads" || variable.Code == "Gads/Ceturksnis")
                //{
                //    entry.Add("TIME");
                //}
                //else if (variable.Code == "Rādītāji" || variable.Code == "Tirgus sektora un ārpus tirgus sektora uzņēmumi")
                //{
                //    entry.Add(subItem.Code);
                //}
                //else if (variable.Code == "Sektors")
                //{
                //    entry.Add(subItem.Code.Split('#')[0]);
                //}
                //else if (variable.Code == "Bruto/ Neto")
                //{
                //    entry.Add(subItem.Code.Split('#')[1]);
                //}
                //else if (variable.Code == "Teritoriālā vienība" || variable.Code == "Administratīvā teritorija")
                //{
                //    entry.Add("AREA");
                //}
                //else
                //{
                //    entry.AddRange(variable.Values);
                //}
                result.Add(entry);
            }

            return result;
        }
    }
}
