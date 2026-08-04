using System.Collections.Generic;
using UnityEngine;

namespace Refactory.UI.GridList
{
    [CreateAssetMenu(fileName = "GridListDatabase", menuName = "TheGoodNightPotion/Grid List/Database")]
    public class GridListDatabase : ScriptableObject
    {
        [SerializeField] private GridListEntryData lockedEntry;
        [SerializeField] private List<GridListCategoryData> categories = new List<GridListCategoryData>();

        public GridListEntryData LockedEntry => lockedEntry;
        public IReadOnlyList<GridListCategoryData> Categories => categories;

        public bool TryGetCategory(GridListCategoryType categoryType, out GridListCategoryData category)
        {
            foreach (GridListCategoryData currentCategory in categories)
            {
                if (currentCategory != null && currentCategory.CategoryType == categoryType)
                {
                    category = currentCategory;
                    return true;
                }
            }

            category = null;
            return false;
        }
    }
}
