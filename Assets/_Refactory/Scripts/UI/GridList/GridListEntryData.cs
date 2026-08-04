using System;
using UnityEngine;

namespace Refactory.UI.GridList
{
    [Serializable]
    public class GridListEntryData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string shortDescription;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite sprite;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private int sceneBuildIndex = -1;

        public string Id => id;
        public string DisplayName => displayName;
        public string ShortDescription => shortDescription;
        public string Description => description;
        public Sprite Sprite => sprite;
        public bool UnlockedByDefault => unlockedByDefault;
        public int SceneBuildIndex => sceneBuildIndex;
        public bool HasScene => sceneBuildIndex >= 0;
    }
}
