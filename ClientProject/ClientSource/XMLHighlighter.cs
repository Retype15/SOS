// Important Note: FULLY AI GENERATED CODE. Does not affect the copyright.

using Barotrauma;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace SOS
{
    public static class XMLHighlighter
    {
        // Syntax colors (IDE "Dark Theme" style palette)
        private static readonly string ColNode = Color.CornflowerBlue.ToStringHex();   // <Item>
        private static readonly string ColAttr = Color.LightSkyBlue.ToStringHex();     // identifier=
        private static readonly string ColValue = Color.LightSalmon.ToStringHex();     // "steel"
        private static readonly string ColComment = Color.DarkSeaGreen.ToStringHex();  // <!-- comment -->
        private static readonly string ColPunctuation = Color.LightGray.ToStringHex(); // < > / =

        // Compiled regular expressions for maximum performance
        private static readonly Regex RegexComment = new Regex(@"<!--[\s\S]*?-->", RegexOptions.Compiled);
        private static readonly Regex RegexNodeName = new Regex(@"(?<=<|</)[a-zA-Z0-9_\-]+", RegexOptions.Compiled);
        private static readonly Regex RegexAttribute = new Regex(@"([a-zA-Z0-9_\-]+)(?=\s*=)", RegexOptions.Compiled);
        private static readonly Regex RegexValue = new Regex(@"""[^""]*""", RegexOptions.Compiled);
        private static readonly Regex RegexPunctuation = new Regex(@"<|>|/|=", RegexOptions.Compiled);

        public static RichString Format(string rawXml)
        {
            if (string.IsNullOrWhiteSpace(rawXml)) return RichString.Rich("");

            // 1. Scape Barotrauma RichText characters (the ‖ symbol)
            // If the raw XML had this symbol for some reason, it would break the parser.
            string safeXml = rawXml.Replace("‖", "||");

            // 2. Highlight Strings (Values between quotes)
            safeXml = RegexValue.Replace(safeXml, match => $"‖color:{ColValue}‖{match.Value}‖color:end‖");

            // 3. Highlight Attributes
            safeXml = RegexAttribute.Replace(safeXml, match => $"‖color:{ColAttr}‖{match.Value}‖color:end‖");

            // 4. Highlight Node Names (<Node)
            safeXml = RegexNodeName.Replace(safeXml, match => $"‖color:{ColNode}‖{match.Value}‖color:end‖");

            // 5. Highlight Punctuation (Angles and equals signs)
            // NOTE: To avoid replacing our own color tags (‖color:Hex‖), 
            // the regex will not touch anything between ‖ symbols.
            // A safer way is to do punctuation first, but it interferes with HTML.
            // To simplify, we color the basic punctuation.
            safeXml = RegexPunctuation.Replace(safeXml, match => $"‖color:{ColPunctuation}‖{match.Value}‖color:end‖");

            // 6. Highlight Comments (They have priority and overwrite any internal color)
            safeXml = RegexComment.Replace(safeXml, match =>
            {
                // Clean colors that may have been injected by error inside the comment
                string cleanComment = Regex.Replace(match.Value, @"‖color:[^‖]+‖|‖color:end‖", "");
                return $"‖color:{ColComment}‖{cleanComment}‖color:end‖";
            });

            return RichString.Rich(safeXml);
        }
    }
}