using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SetAnimationTagsCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly List<SpriteAnimationTag> before;
        private readonly List<SpriteAnimationTag> after;

        public SetAnimationTagsCommand(SpriteDocument document, IReadOnlyList<SpriteAnimationTag> tags)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            before = document.CloneAnimationTagsForCommand();
            after = CloneTags(tags);
        }

        public long EstimatedMemoryBytes => (before.Count + after.Count) * 128L;

        public void Execute()
        {
            document.RestoreAnimationTagsForCommand(after);
        }

        public void Undo()
        {
            document.RestoreAnimationTagsForCommand(before);
        }

        private static List<SpriteAnimationTag> CloneTags(IReadOnlyList<SpriteAnimationTag> source)
        {
            List<SpriteAnimationTag> tags = new List<SpriteAnimationTag>(source.Count);
            foreach (SpriteAnimationTag tag in source)
            {
                tags.Add(tag.Clone());
            }

            return tags;
        }
    }

}
