using System.Collections.Generic;
using Game;
using Level.Shelf;
using UnityEngine;

namespace Managers
{
    public class ShelfManager : MonoBehaviour
    {
        [Header("References")]
        public Transform EnvironmentContainer;

        [Header("Layout Settings")]
        public float ShelfSpacingY;
        public float ItemVisualWidth;
        public float MaxPlayAreaWidth;
        public float MaxPlayAreaHeight;

        public List<ShelfView> ActiveShelves { get; private set; } = new List<ShelfView>();

        public void GenerateShelves(List<ShelfData> shelfDataList)
        {
            ClearShelves();
            EnvironmentContainer.localScale = Vector3.one;

            var totalShelves = shelfDataList.Count;
            var startY = (totalShelves - 1) * ShelfSpacingY / 2f;
            var maxActualWidth = 0f;

            for (var i = 0; i < totalShelves; i++)
            {
                var data = shelfDataList[i];
            
                var currentShelfWidth = data.Width * ItemVisualWidth;
                if (currentShelfWidth > maxActualWidth) maxActualWidth = currentShelfWidth;

                var spawnPos = new Vector3(0, startY - (i * ShelfSpacingY), 0);
                
                var newShelf = GamePools.Instance.ShelfViewPool.Get();
                newShelf.transform.SetParent(EnvironmentContainer);
                newShelf.transform.localPosition = spawnPos;
            
                newShelf.Init(i, data, ItemVisualWidth);
                ActiveShelves.Add(newShelf);
            }

            var totalActualHeight = (totalShelves - 1) * ShelfSpacingY + 2f; 
            var scaleX = MaxPlayAreaWidth / maxActualWidth;
            var scaleY = MaxPlayAreaHeight / totalActualHeight;
            var finalScale = Mathf.Min(scaleX, scaleY, 1f);

            EnvironmentContainer.localScale = new Vector3(finalScale, finalScale, 1f);
        }

        public void ClearShelves()
        {
            foreach (var shelf in ActiveShelves)
            {
                if (shelf != null) 
                    GamePools.Instance.ShelfViewPool.Release(shelf);
            }
            ActiveShelves.Clear();
        }
    } 
}