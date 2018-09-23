using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class HtmlToDashUtil
    {
        public static async Task<DocumentController> ConvertHtmlData(DataPackageView packageView, Point where)
        {
            string html = await packageView.GetHtmlFormatAsync();

            //get url of where this html is coming from
            int htmlStartIndex = html.ToLower().IndexOf("<html", StringComparison.Ordinal); // Edge uses "<HTML", chrome "<html"
            string beforeHtml = html.Substring(0, htmlStartIndex);

            var introParts = beforeHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();
            var uri = packageView.AvailableFormats.Contains("UniformResourceLocator") ? (await packageView.GetWebLinkAsync())?.AbsoluteUri : null;
            uri = uri ?? introParts.LastOrDefault()?.Substring(10);
            
            if (uri?.IndexOf("HTML>") != -1)  // if dropped from Edge, uri is 2nd to last
                uri = introParts[introParts.Count - 2]?.Substring(10);
            string titlesUrl = GetTitlesUrl(uri);

            var text = "";
            if (!string.IsNullOrEmpty(titlesUrl) || uri?.StartsWith("HTML") == true)
            {
                //update html length in intro - the way that word reads HTML is kinda funny
                //it uses numbers in heading that say when html starts and ends, so in order to edit html, 
                //we must change these numbers
                string endingInfo = introParts.ElementAt(2);
                string endingNum = (Convert.ToInt32(endingInfo.Substring(8))).ToString().PadLeft(10, '0');
                introParts[2] = endingInfo.Substring(0, 8) + endingNum;

                string endingInfo2 = introParts.ElementAt(4);
                string endingNum2 = (Convert.ToInt32(endingInfo2.Substring(12))).ToString().PadLeft(10, '0');
                introParts[4] = endingInfo2.Substring(0, 12) + endingNum2;

                string newHtmlStart = string.Join("\r\n", introParts) + "\r\n";

                //get parts so additon is before closing
                int endPoint = html.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);
                string mainHtml = html.Substring(htmlStartIndex, endPoint - htmlStartIndex);
                string htmlClose = html.Substring(endPoint);
                text = ExtractText(mainHtml);

                //combine all parts
                html = newHtmlStart + mainHtml + htmlClose;
            }

            //Overrides problematic in-line styling pdf.js generates, such as transparent divs and translucent elements
            html = string.Concat(html,
                @"<style>
                      div
                      {
                        color: black !important;
                      }
                      html * {
                        opacity: 1.0 !important
                      }
                    </style>"
            );

            var splits = new Regex("<").Split(html);
            var imgs = splits.Where(s => new Regex("img.*src=\"[^>\"]*").Match(s).Length > 0).ToList();
            if (string.IsNullOrEmpty(text) && imgs.Count == 1)
            {
                string srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(imgs.First()).Value;
                string src = srcMatch.Substring(6, srcMatch.Length - 6);
                var imgNote = new ImageNote(new Uri(src), where, new Size(), src);
                imgNote.Document.GetDataDocument().SetField<TextController>(KeyStore.AuthorKey, "HTML", true);
                imgNote.Document.GetDataDocument().SetField<TextController>(KeyStore.SourceUriKey, uri, true);
                imgNote.Document.GetDataDocument().SetField<TextController>(KeyStore.WebContextKey, uri, true);
                imgNote.Document.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);
                return imgNote.Document;
            }

            DocumentController htmlNote;
            SettingsView.WebpageLayoutMode mode = SettingsView.Instance.WebpageLayout;
            SettingsView.WebpageLayoutMode layoutMode = mode == SettingsView.WebpageLayoutMode.Default ? await MainPage.Instance.GetLayoutType() : mode;

            if ((layoutMode == SettingsView.WebpageLayoutMode.HTML && !MainPage.Instance.IsCtrlPressed()) ||
                (layoutMode == SettingsView.WebpageLayoutMode.RTF && MainPage.Instance.IsCtrlPressed()))
            {
                htmlNote = new HtmlNote(html, titlesUrl, where).Document;
            } else
            {
                htmlNote = await CreateRtfNote(where, titlesUrl, html);
            }

            htmlNote.GetDataDocument().SetField<TextController>(KeyStore.SourceUriKey, uri, true);
            htmlNote.GetDataDocument().SetField<TextController>(KeyStore.WebContextKey, uri, true);
            htmlNote.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);

            // this should be put into an operator so that it can be invoked from the scripting language, not automatically from here.
            if (imgs.Any())
            {
                var related = new List<DocumentController>();
                foreach (string img in imgs)
                {
                    string srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(img).Value;

                    if (srcMatch.Length <= 6) continue;

                    string src = srcMatch.Substring(6, srcMatch.Length - 6);
                    var i = new ImageNote(new Uri(src), new Point(), new Size(), src);
                    related.Add(i.Document);
                }

                htmlNote.GetDataDocument().SetField<ListController<DocumentController>>(new KeyController("Html Images", "Html Images"), related, true);
            }

            return htmlNote;
        }
        public static string ExtractText(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var chunks = new List<string>();

            foreach (var item in doc.DocumentNode.DescendantNodesAndSelf())
            {
                if (item.NodeType == HtmlNodeType.Text)
                {
                    if (item.InnerText.Trim() != "")
                    {
                        chunks.Add(item.InnerText.Trim());
                    }
                }
            }
            return string.Join(" ", chunks);
        }

        public static string GetTitlesUrl(string uri)
        {
            // try to get website title
            var uriParts = uri?.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (uriParts == null || uriParts.Count < 2) return "";

            var webNameParts = uriParts[1].Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
            string webName = webNameParts.Count > 2 ? webNameParts[webNameParts.Count - 2] : webNameParts[0];

            webName = new CultureInfo("en-US").TextInfo.ToTitleCase(webName.Replace('_', ' ').Replace('-', ' '));

            string pageTitle = uriParts[uriParts.Count - 1];

            // convert symbols back to correct chars
            pageTitle = Uri.UnescapeDataString(pageTitle);

            // handle complicated google search url
            var googleSearchRes = pageTitle.Split("q=");
            pageTitle = googleSearchRes.Length > 1 ? googleSearchRes[1].Substring(0, googleSearchRes[1].Length - 2).Replace('+', ' ') : pageTitle;

            // check if pageTitle is some id
            bool isId = uriParts.Count > 1 && (pageTitle.Count(x => char.IsDigit(x) || x == '=' || x == '#') > pageTitle.Length / 3 || pageTitle == "index.html");

            pageTitle = isId ? uriParts[uriParts.Count - 2] : pageTitle;
            pageTitle = pageTitle.Contains(".html") || pageTitle.Contains(".aspx") ? pageTitle.Substring(0, pageTitle.Length - 5) : pageTitle;
            pageTitle = pageTitle.Contains(".htm") || pageTitle.Contains(".asp") ? pageTitle.Substring(0, pageTitle.Length - 4) : pageTitle;

            // dashes are used in urls as spaces
            pageTitle = pageTitle.Replace('_', ' ').Replace('-', ' ').Replace('.', ' ');

            // if first word is basically all numbers, its id, so delete
            string firstTitleWord = pageTitle.Split(' ').First();

            bool status = firstTitleWord.Count(char.IsDigit) > firstTitleWord.Length / 2 && pageTitle.Length > firstTitleWord.Length;
            pageTitle = status ? pageTitle.Substring(firstTitleWord.Length + 1) : pageTitle;

            // if last word is basically all numbers, its id, so delete
            string lastTitleWord = pageTitle.Split(' ').Last();

            status = lastTitleWord.Count(char.IsDigit) > lastTitleWord.Length / 2 && pageTitle.Length > lastTitleWord.Length;
            pageTitle = status ?

            pageTitle.Substring(0, pageTitle.Length - lastTitleWord.Length - 1) : pageTitle;
            if (pageTitle.Length > 40)
                pageTitle = pageTitle.Substring(1, 39) + "...";
            else pageTitle = pageTitle.Substring(1);
            pageTitle = char.ToUpper(pageTitle[0]) + pageTitle;

            return $"{webName} ({pageTitle})";
        }

        public static async Task<DocumentController> CreateRtfNote(Point where, string title, string html)
        {
            // copy html to clipboard
            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetHtmlFormat(html);
            Clipboard.SetContent(dataPackage);

            // to import RTF from html, create a ValueSet and call Word app to do the conversion on the clipboard
            var rpcRequest = new ValueSet { { "REQUEST", "HTML to RTF" } };
            await DotNetRPC.CallRPCAsync(rpcRequest);

            DataPackageView dataPackageView = Clipboard.GetContent();

            // if rtf extraction failed...
            if (!dataPackageView.Contains(StandardDataFormats.Rtf))
                return new HtmlNote(html, title, where: where).Document;

            string richtext = await dataPackageView.GetRtfAsync();
            var rtfNote = new RichTextNote(richtext, where, new Size(double.NaN, double.NaN)).Document;
            var text = rtfNote.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
            if (!string.IsNullOrEmpty(title))
                rtfNote.GetDataDocument().SetTitle(title + ": " + text.Split('\v').FirstOrDefault());

            return rtfNote;
        }
    }
}
