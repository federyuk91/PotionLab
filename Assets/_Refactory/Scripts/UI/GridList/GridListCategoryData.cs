using System.Collections.Generic;
using UnityEngine;

namespace Refactory.UI.GridList
{
    [CreateAssetMenu(fileName = "GridListCategory", menuName = "TheGoodNightPotion/Grid List/Category")]
    public class GridListCategoryData : ScriptableObject
    {
        [SerializeField] private GridListCategoryType categoryType;
        [SerializeField] private string title;
        [SerializeField] private List<GridListEntryData> entries = new List<GridListEntryData>();

        public GridListCategoryType CategoryType => categoryType;
        public string Title => title;
        public IReadOnlyList<GridListEntryData> Entries => entries;
    }
}
