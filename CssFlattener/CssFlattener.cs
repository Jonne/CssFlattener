using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.IO;
using log4net;
using System.Reflection;

namespace CssFlattener
{
    public class CssFlattener
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Flatten(string originalHtml, string css)
        {
            if (originalHtml == null)
            {
                throw new ArgumentNullException("originalHtml");
            }

            if (css == null)
            {
                throw new ArgumentNullException("css");
            }

            Log.Debug("Reading css stule rules from css");
            IEnumerable<StyleRule> styleRules = GetStyleRules(css);
            Log.DebugFormat("Found {0} style rules", styleRules.Count());

            Log.Debug("Parsing Html document");
            var document = new HtmlDocument();
            document.LoadHtml(originalHtml);

            Log.Debug("Applying style rules to html elements");
            ApplyOutlineCssToDocument(document, styleRules);

            var sw = new StringWriter();
            document.Save(sw);
            return sw.ToString();
        }

        private static void AddOutlineStylesToInlineStyles(Dictionary<string, string> inlineStyles, Dictionary<string, string> outlineStyles)
        {
            foreach (var outlineStyle in outlineStyles)
            {
                // outline styles overwrite inline styles, not perfect, but good enough.
                inlineStyles[outlineStyle.Key] = outlineStyle.Value;
            }
        }

        private static void ApplyOutlineCssToHtmlNode(HtmlNode node, Dictionary<string, string> outlineStyle)
        {
            string inlineStyle = node.GetAttributeValue("style", String.Empty);
            Dictionary<string, string> inlineStyleAsDictionary = ConvertStyleToDictionary(inlineStyle);

            AddOutlineStylesToInlineStyles(inlineStyleAsDictionary, outlineStyle);

            string mergedCss = string.Join(";", inlineStyleAsDictionary.Select(x => string.Format("{0}:{1}", x.Key, x.Value)).ToArray());

            node.SetAttributeValue("style", mergedCss);
        }

        private static IEnumerable<StyleRule> GetStyleRules(string css)
        {
            string cssWithoutComments = RemoveCommentsFromCss(css);

            MatchCollection matches = Regex.Matches(cssWithoutComments, @"\s*(?<selector>[^{]+){(?<attributes>[^}]+)}");

            var cssSelectors = new List<StyleRule>();

            foreach (Match match in matches)
            {
                string selector = match.Groups["selector"].Value;

                if (!selector.Contains(":"))
                {
                    string[] selectors = selector.Split(',');

                    foreach (string specificSelector in selectors)
                    {
                        var rule = new StyleRule
                        {
                            Selector = specificSelector,
                            Declarations = ConvertStyleToDictionary(match.Groups["attributes"].Value),
                            Index = match.Index
                        };

                        cssSelectors.Add(rule);
                    }
                }
            }

            cssSelectors.Sort(CompareRule);

            return cssSelectors;
        }

        private static int CompareRule(StyleRule first, StyleRule second)
        {
            int firstPrecendence = GetCSSSelectorPrecedence(first.Selector);
            int secondPrecendence = GetCSSSelectorPrecedence(second.Selector);

            if (firstPrecendence == secondPrecendence)
            {
                return (first.Index < second.Index) ? -1 : 1;
            }

            return (firstPrecendence < secondPrecendence) ? -1 : 1;
        }

        private static int GetCSSSelectorPrecedence(string selector)
        {
            int precedence = 0;
            int value = 100;
            var search = new[] { "\\#", "\\.", "" }; // ids: worth 100, classes: worth 10, elements: worth 1

            foreach (string s in search)
            {
                if (selector.Trim().Length == 0)
                {
                    break;
                }

                int num = Regex.Matches(selector, s).Count;

                if (num > 0)
                {
                    selector = Regex.Replace(selector, s + "\\w+", "");
                    precedence += (value * num);
                    value = value / 10;
                }
            }

            return precedence;
        }

        private static string RemoveCommentsFromCss(string css)
        {
            return Regex.Replace(css, @"/\*.*\*/", string.Empty);
        }

        private static Dictionary<string, string> ConvertStyleToDictionary(string style)
        {
            string styleWithoutControlCharacters = style.Replace("\t", "").Replace("\r", "").Replace("\n", "");

            return styleWithoutControlCharacters.Split(';')
                .Where(x => x.Trim().Length > 0)
                .ToDictionary(x => x.Split(':')[0], y => y.Split(':')[1]);
        }

        private static void ApplyOutlineCssToDocument(HtmlDocument document, IEnumerable<StyleRule> styleRules)
        {
            foreach (var styleRule in styleRules)
            {
                string xPathSelector = CssToXPathTransformation.Transform(styleRule.Selector);

                Log.DebugFormat("Transform op css selector: {0}, resulted in xpath selector: {1}", styleRule.Selector, xPathSelector);

                try
                {
                    HtmlNodeCollection nodes = document.DocumentNode.SelectNodes(xPathSelector);

                    if (nodes == null)
                    {
                        Log.DebugFormat("No html elements found with xpath expression: {0}", xPathSelector);
                    }
                    else
                    {
                        Log.DebugFormat("Found {0} html elements found with xpath expression: {1}", nodes.Count, xPathSelector);

                        foreach (HtmlNode node in nodes)
                        {
                            ApplyOutlineCssToHtmlNode(node, styleRule.Declarations);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Css to xpath selector transform resulted in invalid xpath expression", ex);
                }
            }
        }
    }
}
