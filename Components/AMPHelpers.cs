using System;
using DotNetNuke.Entities.Modules;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;

namespace Risdall.Modules.DNN_AMP.Components
{
    public class GoogleAmpConverter
    {
        private readonly string source;

        public GoogleAmpConverter(string source)
        {
            this.source = source;
        }

        public static string Convert(string source)
        {
            var converter = new GoogleAmpConverter(source);
            return converter.Convert();
        }

        public string Convert()
        {
            var result = ReplaceIframeWithLink(source);
            result = UpdateAmpImages(result);
            result = StripStylesAndScripts(result);
            result = ReplaceEmbedWithLink(result);
            result = DivStripper(result);
            return result;
        }

        private string HarshStripper(string markup)
        {
            if (string.IsNullOrEmpty(markup)) return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(markup);

            var acceptableTags = new String[] { "strong", "em", "u", "p", "ul", "ol", "li", "span" };

            var nodes = new Queue<HtmlNode>(doc.DocumentNode.SelectNodes("./*|./text()"));

            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);

                }
            }

            return doc.DocumentNode.InnerHtml;
        }

        private string DivStripper(string markup)
        {

            var doc = GetHtmlDocument(markup);
            var elements = doc.DocumentNode.Descendants("//div[contains(@class,'DnnModule')]");
            foreach (var htmlNode in elements)
            {
                htmlNode.ParentNode.RemoveChild(htmlNode, true); //<-- keepGrandChildren
                markup = htmlNode.OuterHtml;
            }


            return markup;
        }

        private string ReplaceIframeWithLink(string markup)
        {

            var doc = GetHtmlDocument(markup);
            var elements = doc.DocumentNode.Descendants("iframe");
            foreach (var htmlNode in elements)
            {
                if (htmlNode.Attributes["src"] == null)
                {
                    continue;
                }
                var link = htmlNode.Attributes["src"].Value;
                var paragraph = doc.CreateElement("p");
                var text = link; // TODO: This might need to be expanded in the future
                var anchor = doc.CreateElement("a");
                anchor.InnerHtml = text;
                anchor.Attributes.Add("href", link);
                anchor.Attributes.Add("title", text);
                paragraph.InnerHtml = anchor.OuterHtml;

                var original = htmlNode.OuterHtml;
                var replacement = paragraph.OuterHtml;

                markup = markup.Replace(original, replacement);
            }

            return markup;
        }

        private string StripStylesAndScripts(string markup)
        {

            var doc = GetHtmlDocument(markup);


            doc.DocumentNode.Descendants()
                           .Where(n => n.Name == "script" || n.Name == "style" || n.Name == "#comment")
                           .ToList()
                           .ForEach(n => n.Remove());
            var elements = doc.DocumentNode.Descendants();
            foreach (var htmlNode in elements)
            {
                if (htmlNode.Attributes["style"] == null)
                {
                    continue;
                }

                htmlNode.Attributes.Remove(htmlNode.Attributes["style"]);
            }

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            doc.Save(writer);
            return builder.ToString();
        }

        private string ReplaceEmbedWithLink(string markup)
        {

            var doc = GetHtmlDocument(markup);
            var elements = doc.DocumentNode.Descendants("embed");
            foreach (var htmlNode in elements)
            {
                if (htmlNode.Attributes["src"] == null) continue;

                var link = htmlNode.Attributes["src"].Value;
                var paragraph = doc.CreateElement("p");
                var anchor = doc.CreateElement("a");
                anchor.InnerHtml = link;
                anchor.Attributes.Add("href", link);
                anchor.Attributes.Add("title", link);
                paragraph.InnerHtml = anchor.OuterHtml;
                var original = htmlNode.OuterHtml;
                var replacement = paragraph.OuterHtml;

                markup = markup.Replace(original, replacement);
            }

            return markup;
        }

        private string UpdateAmpImages(string markup)
        {

            var doc = GetHtmlDocument(markup);
            var imageList = doc.DocumentNode.Descendants("img");

            const string ampImage = "amp-img";
            if (!imageList.Any())
            {
                return markup;
            }

            if (!HtmlNode.ElementsFlags.ContainsKey("amp-img"))
            {
                HtmlNode.ElementsFlags.Add("amp-img", HtmlElementFlag.Closed);
            }

            foreach (var imgTag in imageList)
            {
                var original = imgTag.OuterHtml;

                var replacement = imgTag.Clone();
                string width = "";
                if (imgTag.Attributes.Contains("style") && imgTag.Attributes["style"].Value.Contains("width:"))
                {
                    string Pattern = @"(?<=width: )[0-9A-Za-z]+(?=;)";
                    width = Regex.Match(replacement.OuterHtml, Pattern).Value;
                    width = width.Replace("px", "");
                }

                width = string.IsNullOrEmpty(width) ? "400" : width;

                string height = "";

                if (imgTag.Attributes.Contains("style") && imgTag.Attributes["style"].Value.Contains("height:"))
                {
                    string Pattern = @"(?<=height: )[0-9A-Za-z]+(?=;)";
                    height = Regex.Match(replacement.OuterHtml, Pattern).Value;
                    height = width.Replace("px", "");
                }

                height = string.IsNullOrEmpty(height) ? "300" : height;

                replacement.Name = ampImage;
                replacement.Attributes.Add("width", width);
                replacement.Attributes.Add("height", height);
                replacement.Attributes.Add("layout", "responsive");
                replacement.Attributes.Remove("caption");
                replacement.Attributes.Remove("style");
                replacement.Attributes.Remove("title");
                //replacement.Attributes.Add("test", Result);
                markup = markup.Replace(original, replacement.OuterHtml);
            }

            return markup;
        }

        private HtmlDocument GetHtmlDocument(string htmlContent)
        {
            var doc = new HtmlDocument
            {
                OptionOutputAsXml = false, //don't xml because it puts xml declaration in the string, which amp does not like
                OptionDefaultStreamEncoding = Encoding.UTF8
            };
            doc.LoadHtml(htmlContent);

            return doc;
        }
    }
}