using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Refactory.UI
{
    [CreateAssetMenu(menuName = "The Good Night Potion/UI/Text Color Palette")]
    public sealed class UITextColorPalette : ScriptableObject
    {
        [Serializable]
        private sealed class TermColor
        {
            public string label;
            public string[] terms;
            public Color color = Color.white;
        }

        [SerializeField] private TermColor[] rules;
        private Regex matcher;
        public int Revision { get; private set; }

        private void OnEnable() { matcher = null; Revision++; }
        private void OnValidate() { matcher = null; Revision++; }

        private void EnsureMatcher()
        {
            if (matcher != null) return;
            StringBuilder pattern = new StringBuilder("<[^>]*>");
            if (rules != null)
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i] == null || rules[i].terms == null || rules[i].terms.Length == 0) continue;
                    string[] terms = Array.FindAll(rules[i].terms, term => !string.IsNullOrWhiteSpace(term));
                    if (terms.Length == 0) continue;
                    string[] escaped = Array.ConvertAll(terms, term => Regex.Escape(term.Trim()));
                    pattern.Append($"|(?<r{i}>\\b(?:[0-9]+(?:\\.[0-9]+)?\\s+)?(?:{string.Join("|", escaped)})\\b)");
                }
            matcher = new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        // Source indices match TMP_CharacterInfo.index, including rich-text tags.
        public Color32[] BuildSourceColors(string source, Color defaultColor)
        {
            source = source ?? string.Empty;
            Color32[] colors = new Color32[source.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = defaultColor;
            EnsureMatcher();
            foreach (Match match in matcher.Matches(source))
            {
                if (source[match.Index] == '<') continue;
                for (int rule = 0; rules != null && rule < rules.Length; rule++)
                {
                    if (!match.Groups["r" + rule].Success) continue;
                    for (int i = match.Index; i < match.Index + match.Length; i++) colors[i] = rules[rule].color;
                    break;
                }
            }
            return colors;
        }
    }
}
