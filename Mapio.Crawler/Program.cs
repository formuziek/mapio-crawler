using Mapio.Crawler.Dto;
using Mapio.Dto.Configuration;
using Mapio.Dto.Enums;
using Mapio.Shared.Enums;
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
        /// <summary>
        /// Switch for disabling crawling.
        /// </summary>
        private static bool _offlineMode = true;

        private const string _rootUri = "https://data.stat.gov.lv/api/v1/lv/OSP_PUB/";
        private static List<DataSet> _filterOutput = new List<DataSet>();

        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.LatinExtendedA),
        };

        /// <summary>
        /// The crawl script. From the API base crawls through all indexed endpoints and gathers all tables.
        /// </summary>
        public static async Task<List<Block>> Crawl()
        {
            if (_offlineMode)
            {
                return null;
            }

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
                // Avoiding too many requests.
                await Task.Delay(2000);
                var blockHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(httpClient.BaseAddress, block.Id),
                    Method = HttpMethod.Get,
                };
                var blockHttpResponse = await httpClient.SendAsync(blockHttpRequest);
                block.Children = JsonSerializer.Deserialize<List<Block>>(await blockHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
                
                foreach (var child in block.Children)
                {
                    await Recurse(child, blockHttpRequest.RequestUri, httpClient);
                }
            }

            // Serializing and storing raw crawl output in case we want to run filter later without waiting for the indexing.
            var data = JsonSerializer.Serialize(blocks, _jsonSerializerOptions);
            System.IO.File.WriteAllText("rawBlocks.json", data);

            return blocks;
        }

        private static async Task Recurse(Block child, Uri lastUri, HttpClient httpClient)
        {
            // Avoiding too many requests.
            await Task.Delay(2000);

            // If the type is "l" (Level, probably), moving to next children.
            if (child.Type == "l")
            {
                var levelHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{lastUri.OriginalString}/{child.Id}"),
                    Method = HttpMethod.Get,
                };
                var levelHttpResponse = await httpClient.SendAsync(levelHttpRequest);
                child.Children = JsonSerializer.Deserialize<List<Block>>(await levelHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
                foreach (var subLevel in child.Children)
                {
                    await Recurse(subLevel, levelHttpRequest.RequestUri, httpClient);
                }
            }

            // If the type is "t" (Table), we have hit the target, mapping the data and finishing.
            if (child.Type == "t")
            {
                var tableHttpRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{lastUri.OriginalString}/{child.Id}"),
                    Method = HttpMethod.Get,
                };
                var tableHttpResponse = await httpClient.SendAsync(tableHttpRequest);
                child.Table = JsonSerializer.Deserialize<Response>(await tableHttpResponse.Content.ReadAsStringAsync(), _jsonSerializerOptions);
            }
        }

        /// <summary>
        /// Filters the raw output to finish with valid blocks only, mapped to web format.
        /// </summary>
        /// <remarks>
        /// Valid blocks - the blocks which contain area data which web can use.
        /// Web format - slightly simplified format with some additional information for web.
        /// </remarks>
        public static async Task Filter(List<Block> blocks)
        {
            if (blocks == null)
            {
                var dataRaw = await System.IO.File.ReadAllTextAsync("rawBlocks.json");
                blocks = JsonSerializer.Deserialize<List<Block>>(dataRaw, _jsonSerializerOptions);
            }

            foreach (var block in blocks)
            {
                foreach (var child in block.Children)
                {
                    RecurseFilter(child, string.Empty);
                }
            }

            System.IO.File.WriteAllText("filteredBlocks.json", JsonSerializer.Serialize(_filterOutput, _jsonSerializerOptions));
        }

        private static void RecurseFilter(Block block, string previousUri)
        {
            if (block.Type == "l")
            {
                foreach (var child in block.Children)
                {
                    RecurseFilter(child, string.Join("/", previousUri, block.Id));
                }
            }

            if (block.Type == "t")
            {
                string uri = string.Join("/", previousUri, block.Id);
                var areaCode = block.Table.Variables.FirstOrDefault(c => c.Code == "AREA");
                if (areaCode == null || areaCode.Values == null)
                {
                    return;
                }

                bool containsAreaCodes = true;
                bool containsNewAreaCodes = true;
                foreach (var aCodeOld in Enum.GetNames(typeof(AdministrativeCodes)))
                {
                    if (!areaCode.Values.Contains(aCodeOld))
                    {
                        containsAreaCodes = false;
                        break;
                    }
                }

                foreach (var aCodeNew in Enum.GetNames(typeof(AdministrativeCodes2021)))
                {
                    if (!areaCode.Values.Contains(aCodeNew))
                    {
                        containsNewAreaCodes = false;
                        break;
                    }
                }

                if (!containsAreaCodes && !containsNewAreaCodes)
                {
                    return;
                }

                var dataSet = new DataSet
                {
                    Version = containsNewAreaCodes ? "AFTER_2021_ATR" : "BEFORE_2021_ATR",
                    Text = block.Text,
                    Uri = uri,
                    Variables = MapVariables(block.Table.Variables),
                };

                _filterOutput.Add(dataSet);
            }
        }

        private static List<Mapio.Dto.Configuration.Variable> MapVariables(List<Dto.Variable> variables)
        {
            var output = new List<Mapio.Dto.Configuration.Variable>();
            foreach (var variable in variables)
            {
                var mappedVariable = new Mapio.Dto.Configuration.Variable
                {
                    Code = variable.Code,
                    Text = variable.Text,
                };

                var valueItems = new List<ValueItem>();
                for (int i = 0; i < variable.Values.Count; i++)
                {
                    valueItems.Add(new ValueItem { Text = variable.ValueTexts[i], Value = variable.Values[i] });
                }

                mappedVariable.ValueItems = valueItems;

                if (mappedVariable.Code == "AREA")
                {
                    mappedVariable.ValueItems = null;
                }

                if (mappedVariable.Code == "TIME")
                {
                    mappedVariable.ValueItems.RemoveAll(x => !int.TryParse(x.Value, out int year) || year < 2009);
                    if (mappedVariable.ValueItems.Count == 0)
                    {
                        continue;
                    }
                }

                output.Add(mappedVariable);
            }

            return output;
        }

        /// <summary>
        /// The main version of the script, retrieves all unique data points from a predefined list of endpoints.
        /// </summary>
        public static async Task Main(string[] args)
        {
            var blocks = await Crawl();

            await Filter(blocks);

            return;
        }
    }
}
