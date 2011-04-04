using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CssFlattener
{
    public class CssToXPathTransformation
    {
        public static IEnumerable<CssToXPathTransformation> All = new List<CssToXPathTransformation>
        { 
            new CssToXPathTransformation(@"\s+>\s+", "/"), // Matches any F element that is a child of an element E
            new CssToXPathTransformation(@"(\w+)\s+\+\s+(\w+)", "$1/following-sibling::*[1]/self::$2"), // Matches any F element that is a child of an element E.
            new CssToXPathTransformation(@"\s+", "//"),  // Matches any F element that is a descendant of an E element.
            new CssToXPathTransformation(@"(\w)\[(\w+)\]", "$1[@$2]"), // Matches element with attribute
            new CssToXPathTransformation(@"(\w)\[(\w+)\=[\'""]?(\w+)[\'""]?\]", "$1[@$2=\"$3\"]"), // Matches element with EXACT attribute
            new CssToXPathTransformation(@"(\w+)\#([\w]+)", "$1[@id='$2']"), // Matches id attributes
            new CssToXPathTransformation(@"(?<!\w)\#([\w]+)", "*[@id='$1']"), // Matches id attributes without element type
            new CssToXPathTransformation(@"(\w+|\*+)?((\.[\w\-]+)+)", MatchClass), // Matches class attributes 
        };

        public CssToXPathTransformation(string regularExpression, string replacementExpression)
        {
            RegularExpression = regularExpression;
            ReplacementExpression = replacementExpression;
        }

        public CssToXPathTransformation(string regularExpression, MatchEvaluator matchEvaluator)
        {
            RegularExpression = regularExpression;
            MatchEvaluator = matchEvaluator;
        }

        public string RegularExpression
        {
            get;
            set;
        }

        public string ReplacementExpression
        {
            get;
            set;
        }

        public MatchEvaluator MatchEvaluator
        {
            get;
            set;
        }

        private static string MatchClass(Match match)
        {
            string[] matches = match.Value.Split('.');

            string element = matches[0].Length == 0 ? "*" : matches[0];

            var sb = new StringBuilder();
            sb.Append(element);

            for (int index = 1; index < matches.Length; index++)
            {
                sb.AppendFormat(@"[contains(concat(' ', @class, ' '), concat(' ', '{0}', ' '))]", matches[index]);
            }

            return sb.ToString();
        }

        public string TransformToXPath(string cssSelector)
        {
            if (MatchEvaluator != null)
            {
                return Regex.Replace(cssSelector, RegularExpression, MatchEvaluator);
            }

            return Regex.Replace(cssSelector, RegularExpression, ReplacementExpression);
        }

        public static string Transform(string cssSelector)
        {
            string xPathSelector = cssSelector.Trim();

            foreach (CssToXPathTransformation cssToXpathTransformation in All)
            {
                xPathSelector = cssToXpathTransformation.TransformToXPath(xPathSelector);
            }

            return "//" + xPathSelector.TrimEnd();
        }
    }
}
