using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class BrushStrokeService
    {
        private readonly Dictionary<Vector2Int, PixelChange> changes = new Dictionary<Vector2Int, PixelChange>();

        public void Begin()
        {
            changes.Clear();
        }

        public void Record(SpriteCel cel, Vector2Int position, Color color)
        {
            if (cel == null || !cel.Contains(position))
            {
                return;
            }

            if (changes.TryGetValue(position, out PixelChange existingChange))
            {
                if (SpriteColorMath.AreEqual(existingChange.PreviousColor, color))
                {
                    changes.Remove(position);
                    return;
                }

                changes[position] = new PixelChange(position, existingChange.PreviousColor, color);
                return;
            }

            Color previousColor = cel.GetPixel(position);
            if (!SpriteColorMath.AreEqual(previousColor, color))
            {
                changes[position] = new PixelChange(position, previousColor, color);
            }
        }
        public bool TryGetChange(Vector2Int position, out PixelChange change)
        {
            return changes.TryGetValue(position, out change);
        }

        public void Remove(Vector2Int position)
        {
            changes.Remove(position);
        }

        public void Cancel()
        {
            changes.Clear();
        }

        public SpriteToolboxOperationResult Commit()
        {
            List<PixelChange> result = new List<PixelChange>(changes.Values);
            changes.Clear();
            return SpriteToolboxOperationResult.FromPixelChanges(result);
        }
    }
}
