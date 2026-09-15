using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [CreateAssetMenu(menuName = "Federico Tools/Sprite Toolbox/Document", fileName = "NewSpriteDocument")]
    public sealed class SpriteDocumentAsset : ScriptableObject
    {
        [SerializeReference] private SpriteDocument document;

        public SpriteDocument Document => document;

        public void Initialize(int width, int height)
        {
            document = new SpriteDocument(width, height);
        }

        public void ReplaceDocument(SpriteDocument newDocument)
        {
            document = newDocument;
        }
    }
}
