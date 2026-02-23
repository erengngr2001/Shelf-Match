using System;
using System.Collections.Generic;
using Level.Shelf;
using UnityEngine;

namespace Managers
{
    public class ShelfManager : MonoBehaviour
    {
        [Header("References")]
        public ShelfView ShelfPrefab;
        public Transform EnvironmentContainer;

        [Header("Layout Settings")]
        public float ShelfSpacingY;
        public float ItemVisualWidth;

        public List<ShelfView> ActiveShelves { get; private set; } = new List<ShelfView>();

        public float MaxPlayAreaWidth;
        public float MaxPlayAreaHeight;

        public void LoadLevel(int level)
        {
            ClearShelves();
            
            var levelFile = Resources.Load<TextAsset>($"Levels/level{level}");
            if (levelFile == null)
            {
                Debug.LogError($"level{level} not found in Resources/Levels!");
                return;
            }

            var parsedData = ParseLevelData(levelFile.text);

            GenerateShelves(parsedData);
        }

        // Parse the text (e.g., "[3,2],[3,2],[3,2]")
        private List<ShelfData> ParseLevelData(string rawText)
        {
            var dataList = new List<ShelfData>();
            
            var cleanText = rawText.Trim().Replace(" ", "");
            var shelfStrings = cleanText.Split(new[] { "],[" }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in shelfStrings)
            {
                var cleanPair = s.Replace("[", "").Replace("]", "");
                var values = cleanPair.Split(',');

                if (values.Length == 2 && 
                    int.TryParse(values[0], out int width) && 
                    int.TryParse(values[1], out int layers))
                    dataList.Add(new ShelfData(width, layers));
            }

            return dataList;
        }

        private void GenerateShelves(List<ShelfData> shelfDataList)
        {
            EnvironmentContainer.localScale = Vector3.one;

            var totalShelves = shelfDataList.Count;
            
            // Calculate the starting Y position so the entire block of shelves is vertically centered
            var startY = (totalShelves - 1) * ShelfSpacingY / 2f;
            var maxActualWidth = 0f;

            for (var i = 0; i < totalShelves; i++)
            {
                var data = shelfDataList[i];
            
                // Track the widest shelf to know our horizontal bounds
                var currentShelfWidth = data.Width * ItemVisualWidth;
                if (currentShelfWidth > maxActualWidth)
                    maxActualWidth = currentShelfWidth;

                // Calculate position
                var spawnPos = new Vector3(0, startY - (i * ShelfSpacingY), 0);
                
                // Instantiate
                // todo: pool as next step
                var newShelf = Instantiate(ShelfPrefab, spawnPos, Quaternion.identity, EnvironmentContainer);
                newShelf.gameObject.name = $"Shelf_{i}";
            
                // Initialize
                newShelf.Init(i, data, ItemVisualWidth);
                ActiveShelves.Add(newShelf);
            }

            var totalActualHeight = (totalShelves - 1) * ShelfSpacingY + 2f; 

            var scaleX = MaxPlayAreaWidth / maxActualWidth;
            var scaleY = MaxPlayAreaHeight / totalActualHeight;

            var finalScale = Mathf.Min(scaleX, scaleY, 1f);

            EnvironmentContainer.localScale = new Vector3(finalScale, finalScale, 1f);
        }

        private void ClearShelves()
        {
            foreach (var shelf in ActiveShelves)
            {
                // todo: pool as next step
                if (shelf != null)
                    Destroy(shelf.gameObject);
            }
            
            ActiveShelves.Clear();
        }
    } 
}

