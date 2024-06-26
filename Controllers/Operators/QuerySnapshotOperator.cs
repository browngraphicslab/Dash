﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Windows.Web.Http;
using HtmlAgilityPack;
using static System.String;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.query, Op.Name.q)]
    public sealed class QuerySnapshotOperator : OperatorController
    {
        public static readonly KeyController QueryKey = KeyController.Get("Query");

        public static readonly KeyController ResultKey = KeyController.Get("Result");

        public QuerySnapshotOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("QuerySnapshotOperator");

        public override FieldControllerBase GetDefaultController() => new QuerySnapshotOperator();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Text
        };

        public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var query = (inputs[QueryKey] as TextController)?.Data;

            if (!IsNullOrEmpty(query))
            {
                var answer = await QueryGoogle(query);
                if (answer != null)
                {
                    outputs[ResultKey] = new TextController(answer);
                }
            }
        }
        public static async Task<string> QueryGoogle(string query)
        {
            var httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;
            if (!headers.UserAgent.TryParseAdd(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.8; rv:21.0) Gecko/20100101 Firefox/21.0"))
                throw new Exception("Invalid header value: Dash");

            try
            {
                var httpResponse =
                        await httpClient.GetAsync(
                            new Uri("https://www.google.com/search?q=" + query.Replace(' ', '+')));
                httpResponse.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await httpResponse.Content.ReadAsStringAsync());

                // IF CONVERSION TABLE i.e. query("how many feet in a mile?")
                var results = doc.DocumentNode.SelectNodes(".//input[./ancestor::div[@id='NotFQb']]");
                if (results != null)
                {
                    return results[0].GetAttributeValue("value", "");
                }

                // IF GRAPH i.e. query("gdp of usa")
                results = doc.DocumentNode.SelectNodes(".//div[contains(@class, 'kpd-ans kno-fb-ctx KBXm4e')]");
                if (results != null)
                {
                    return results[0].InnerText;
                }

                // IF CONSTANT i.e. query("speed of light")
                results = doc.DocumentNode.SelectNodes(".//div[contains(@class, 'dDoNo vk_bk')]");
                if (results != null)
                {
                    var text = results[0].InnerText;
                    var outText = text.Contains('=') ? text.Split('=')[1].Trim() : text;
                    return outText;
                }

                // IF SUMMARY i.e. query("how many times was a star is born made?")
                results = doc.DocumentNode.SelectNodes(".//span[contains(@class, 'ILfuVd c3biWd')]");
                if (results == null) results = doc.DocumentNode.SelectNodes(".//span[contains(@class, 'ILfuVd')]");
                if (results != null)
                {
                    return results[0].InnerText.Replace("&#39;", "'");
                }

                // IF CARD i.e. query("how many world series yankees")
                results = doc.DocumentNode.SelectNodes(".//div[contains(@class, 'Z0LcW')]");
                if (results != null)
                {
                    return results[0].InnerText;
                }

                // IF DICTIONARY DEFINITION i.e. query("terminal velocity")
                results = doc.DocumentNode.SelectNodes(".//span[./ancestor::div[contains(@style, 'display:inline')]]");
                if (results != null)
                {
                    var finalText = results.Last().InnerText.Replace("&#39;", "'");
                    var partOfSpeech = doc.DocumentNode.SelectNodes("//span[./ancestor::i]");
                    if (partOfSpeech != null) finalText = partOfSpeech[0].InnerText.Replace("&#39;", "'") + ": " + finalText;
                    return finalText;
                }

                // OTHERWISE CAPTURE TEXT OF FIRST PREVIEW SNIPPET i.e. query("how many times was a star is born made?")
                results = doc.DocumentNode.SelectNodes(".//span[contains(@class, 'st')]");
                if (results != null)
                {
                    return results[0].InnerText.Replace("&#39;", "'");
                }

                return $"No snapshot for \'{query}\' - search for something more specific.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message);
            }
            return null;
        }
    }
}
