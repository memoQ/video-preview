using System;

namespace MemoQ.VideoPreview
{
    public class Document
    {
        public Document(Guid id, string name, string media)
        {
            Id = id;
            Name = name;
            Media = media;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Media { get; }
    }
}
