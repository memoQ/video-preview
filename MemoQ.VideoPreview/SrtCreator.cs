using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MemoQ.VideoPreview
{
    internal static class SrtFileCreator
    {
        private static Regex tagRegex = new Regex(@"<(?<isClosingTag>/?)(?<tagName>b|i|u)>");
        private static string spaces = "( |\u00a0|\u1680|\u2000|\u2001|\u2002|\u2003|\u2004|\u2005|\u2006|\u2007|\u2008|\u2009|\u200a|\u200b|\u2060|\u3000)";
        private static Regex whitespaceOutsideTagsRegex = new Regex($@"(?<closingTags>(</(b|i|u)>)+)(?<whitespaces>({spaces}+))(?<openingTags>(<(b|i|u)>)+)");
        private static Regex onlyWhitespaceTagsRegex = new Regex($@"(?<openingTags>(<(b|i|u)>)+)(?<whitespaces>({spaces}+))(?<closingTags>(</(b|i|u)>)+)");

        /// <summary>
        /// Saves an srt file from the given <paramref name="subtitles"/> into the given <paramref name="filename"/>.
        /// </summary>
        /// <param name="subtitles">Cannot be null and must be ordered by start then end time.</param>
        /// <param name="filename">Cannot be null and must be a full path that exists.</param>
        public static void Create(IEnumerable<Subtitle> subtitles, string filename)
        {
            if (subtitles == null)
                throw new ArgumentNullException(nameof(subtitles));
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            var i = 1;
            var sb = new StringBuilder();
            foreach (var subtitle in subtitles)
            {
                var startTime = TimeSpan.FromMilliseconds(subtitle.Start);
                var endTime = TimeSpan.FromMilliseconds(subtitle.End);
                sb.AppendLine(i.ToString());
                sb.AppendLine(startTime.ToString(@"hh\:mm\:ss\,fff", new CultureInfo("fr-FR")) + " --> " + endTime.ToString(@"hh\:mm\:ss\,fff", new CultureInfo("fr-FR")));
                sb.AppendLine(getTextForSubtitle(subtitle));
                sb.AppendLine();
                i++;
            }

            File.WriteAllText(filename, sb.ToString());
        }

        /// <summary>
        /// Gets the subtitle text (and handles overlapped formattings)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string getTextForSubtitle(Subtitle text)
        {
            if (!text.HasFormatting)
                return text.Text;

            return stickWhitespacesToText(handleOverlappedFormattingTags(text.Text));
        }

        /// <summary>
        /// Helper method to fix overlapping formatting issues 
        /// (unfortunately VLC control cannot show overlapped formattings, so we have to fix these by closing and reopening those overlappings)
        /// </summary>
        private static string handleOverlappedFormattingTags(string text)
        {
            var openedTagNames = new List<string>();
            var closedTagNames = new List<string>();
            int index = 0;
            while (index < text.Length)
            {
                var match = tagRegex.Match(text, index);
                if (!match.Success)
                    break;

                int insertedCharsBefore = 0;
                int insertedCharsAfter = 0;
                if (match.Groups["isClosingTag"].Length > 0)
                {
                    // if a closing tag was found, close every opened tag before (in reversed order)
                    string closedTagName = match.Groups["tagName"].Value;
                    for (int i = openedTagNames.Count - 1; i >= 0; i--)
                    {
                        var openedTagName = openedTagNames[i];

                        // if the closing tag is reached, we are fine
                        if (closedTagName == openedTagName)
                            break;

                        var closeOpenedTag = $"</{openedTagName}>";
                        var reOpenOpenedTag = $"<{openedTagName}>";
                        // close the opened tag before the current closing tag
                        text = text.Insert(match.Index + insertedCharsBefore, closeOpenedTag);
                        insertedCharsBefore += closeOpenedTag.Length;
                        // reopen the same tag we just closed after the current closing tag
                        text = text.Insert(match.Index + insertedCharsBefore + match.Length, reOpenOpenedTag);
                        insertedCharsAfter += reOpenOpenedTag.Length;
                    }
                    openedTagNames.Remove(closedTagName);
                }
                else
                {
                    // collect the opened tag
                    string openedTagName = match.Groups["tagName"].Value;
                    if (!openedTagNames.Contains(openedTagName))
                        openedTagNames.Add(openedTagName);
                }
                index = match.Index + insertedCharsBefore + match.Length + insertedCharsAfter;
            }
            return text;
        }

        /// <summary>
        /// Helper method to fix whitespace issues 
        /// (unfortunately VLC control cannot show spaces in some cases, so we have to stick them to texts)
        /// </summary>
        private static string stickWhitespacesToText(string text)
        {
            string onlyWhitespaceTagsCleared = text;
            int index = 0;
            while (index < onlyWhitespaceTagsCleared.Length)
            {
                var onlyWhitespaceMatch = onlyWhitespaceTagsRegex.Match(onlyWhitespaceTagsCleared, index);
                if (!onlyWhitespaceMatch.Success)
                    break;

                // collect the opening tags
                var openingTagMatches = tagRegex.Matches(onlyWhitespaceMatch.Groups["openingTags"].Value);
                var openingTags = new List<string>();
                foreach (Match openingTagMatch in openingTagMatches)
                {
                    openingTags.Add(openingTagMatch.Groups["tagName"].Value);
                }

                // see whether every opening tag was closed and only them
                var closingTagMatches = tagRegex.Matches(onlyWhitespaceMatch.Groups["closingTags"].Value);
                bool closeTagWasNotOpened = false;
                foreach (Match closingTagMatch in closingTagMatches)
                {
                    if (!openingTags.Remove(closingTagMatch.Groups["tagName"].Value))
                    {
                        closeTagWasNotOpened = true;
                        break;
                    }
                }

                // if only whitespaces were inside tags, move them after the tags, or drop the open-close tags if they are in sync
                if (closeTagWasNotOpened || openingTags.Any())
                {
                    onlyWhitespaceTagsCleared = onlyWhitespaceTagsRegex.Replace(onlyWhitespaceTagsCleared, "${openingTags}${closingTags}${whitespaces}", 1, onlyWhitespaceMatch.Index);
                    index = onlyWhitespaceMatch.Index + onlyWhitespaceMatch.Length;
                }
                else
                {
                    onlyWhitespaceTagsCleared = onlyWhitespaceTagsRegex.Replace(onlyWhitespaceTagsCleared, "${whitespaces}", 1, onlyWhitespaceMatch.Index);
                    index = onlyWhitespaceMatch.Index + onlyWhitespaceMatch.Groups["whitespaces"].Length;
                }
            }

            // if only whitespaces were between tags, move them before the tags
            return whitespaceOutsideTagsRegex.Replace(onlyWhitespaceTagsCleared, "${whitespaces}${closingTags}${openingTags}");
        }
    }
}
