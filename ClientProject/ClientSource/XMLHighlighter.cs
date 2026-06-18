// Important Note: FULLY AI GENERATED CODE. Does not affect the copyright.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Text.RegularExpressions;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public static partial class XMLHighlighter
    {
        // Syntax colors (IDE "Dark Theme" style palette)
        private static readonly string ColNode = Color.CornflowerBlue.ToStringHex();   // <Item>
        private static readonly string ColAttr = Color.LightSkyBlue.ToStringHex();     // identifier=
        private static readonly string ColValue = Color.LightSalmon.ToStringHex();     // "steel"
        private static readonly string ColComment = Color.DarkSeaGreen.ToStringHex();  // <!-- comment -->
        private static readonly string ColPunctuation = Color.LightGray.ToStringHex(); // < > / =

        public static RichString Format(string rawXml)
        {
            if (string.IsNullOrWhiteSpace(rawXml)) return RichString.Rich("");

            // 1. Scape Barotrauma RichText characters (the ‖ symbol)
            // If the raw XML had this symbol for some reason, it would break the parser.
            string safeXml = rawXml.Replace("‖", "||");

            // 2. Highlight Strings (Values between quotes)
            safeXml = RegexValue().Replace(safeXml, match => $"‖color:{ColValue}‖{match.Value}‖color:end‖");

            // 3. Highlight Attributes
            safeXml = RegexAttribute().Replace(safeXml, match => $"‖color:{ColAttr}‖{match.Value}‖color:end‖");

            // 4. Highlight Node Names (<Node)
            safeXml = RegexNodeName().Replace(safeXml, match => $"‖color:{ColNode}‖{match.Value}‖color:end‖");

            // 5. Highlight Punctuation (Angles and equals signs)
            // NOTE: To avoid replacing our own color tags (‖color:Hex‖), 
            // the regex will not touch anything between ‖ symbols.
            // A safer way is to do punctuation first, but it interferes with HTML.
            // To simplify, we color the basic punctuation.
            safeXml = RegexPunctuation().Replace(safeXml, match => $"‖color:{ColPunctuation}‖{match.Value}‖color:end‖");

            // 6. Highlight Comments (They have priority and overwrite any internal color)
            safeXml = RegexComment().Replace(safeXml, match =>
            {
                // Clean colors that may have been injected by error inside the comment
                string cleanComment = CleanComment().Replace(match.Value, "");
                return $"‖color:{ColComment}‖{cleanComment}‖color:end‖";
            });

            return RichString.Rich(safeXml);
        }

        [GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.Compiled)]
        private static partial Regex RegexComment();
        [GeneratedRegex(@"(?<=<|</)[a-zA-Z0-9_\-]+", RegexOptions.Compiled)]
        private static partial Regex RegexNodeName();
        [GeneratedRegex(@"([a-zA-Z0-9_\-]+)(?=\s*=)", RegexOptions.Compiled)]
        private static partial Regex RegexAttribute();
        [GeneratedRegex(@"""[^""]*""", RegexOptions.Compiled)]
        private static partial Regex RegexValue();
        [GeneratedRegex(@"<|>|/|=", RegexOptions.Compiled)]
        private static partial Regex RegexPunctuation();
        [GeneratedRegex(@"‖color:[^‖]+‖|‖color:end‖")]
        private static partial Regex CleanComment();
    }
}