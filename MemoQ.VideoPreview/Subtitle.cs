using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MemoQ.VideoPreview
{
    [DebuggerDisplay("{ddt,nq}")]
    public class Subtitle
    {
        private string formattingTagPattern = "<b>|</b>|<i>|</i>|<u>|</u>";

        public Subtitle(long start, long end, string text)
        {
            Start = start;
            End = end;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            HasFormatting = Regex.IsMatch(text, formattingTagPattern);
            PlainText = HasFormatting ? getPlainText(text) : text;
        }

        public long Start { get; }
        public long End { get; }
        public string Text { get; }
        public string PlainText { get; }
        public bool HasFormatting { get; }

        private string getPlainText(string text)
        {
            return Regex.Replace(text, formattingTagPattern, "");
        }

        /// <summary>
        /// Used to produce a friendly display in the debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string ddt => $"{Start}-{End}: {Text}";
    }
}
