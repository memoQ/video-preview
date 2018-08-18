using MemoQ.PreviewInterfaces.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Represents a segment part in the video preview tool.
    /// </summary>
    public class SegmentPart
    {
        public readonly string PreviewPartId;
        public readonly PreviewProperty[] PreviewProperties;
        public readonly string SourceLangCode;
        public readonly string TargetLangCode;
        public readonly string SourceContent;
        public readonly string TargetContent;
        public readonly Subtitle Subtitle;

        public SegmentPart(string previewPartId, PreviewProperty[] previewProperties,
            string sourceLang, string targetLang, string sourceContent, string targetContent, Subtitle subtitle)
        {
            PreviewPartId = previewPartId;
            PreviewProperties = previewProperties;
            SourceLangCode = sourceLang;
            TargetLangCode = targetLang;
            SourceContent = sourceContent;
            TargetContent = targetContent;
            Subtitle = subtitle;
        }
    }
}
