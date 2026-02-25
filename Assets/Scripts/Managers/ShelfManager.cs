using System.Collections.Generic;
using Game;
using Level.Objects;
using Level.Shelf;
using UnityEngine;

namespace Managers
{
    public class ShelfManager : MonoBehaviour, IManualUpdate
    {
        [Header("References")]
        public Transform EnvironmentContainer;

        [Header("Layout Settings")]
        public float ShelfSpacingY;
        public float ItemVisualWidth;
        public float MaxPlayAreaWidth;
        public float MaxPlayAreaHeight;

        [Header("Visual Settings")]
        public float ItemOffsetY;
        public Vector3 LayerOffset;

        public List<ShelfView> ActiveShelves { get; private set; } = new List<ShelfView>();

        public void ManualUpdate()
        {
            // Frame-by-frame shelf logic (animations etc.) will go here
        }
        
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
                newShelf.SetupGrid();
                ActiveShelves.Add(newShelf);
            }

            var totalActualHeight = (totalShelves - 1) * ShelfSpacingY + 2f; 
            var scaleX = MaxPlayAreaWidth / maxActualWidth;
            var scaleY = MaxPlayAreaHeight / totalActualHeight;
            var finalScale = Mathf.Min(scaleX, scaleY, 1f);

            EnvironmentContainer.localScale = new Vector3(finalScale, finalScale, 1f);
        }

        public void ExtractObjectFromShelf(ShelfView shelf, ObjectView obj)
        {
            shelf.Grid[obj.GridX, obj.LayerIndex] = null;
            obj.SetState(ObjectState.MovingToStack);
            
            UpdateShelfVisuals(shelf, true);
        }

        public void UndoObjectPlacementOnShelf(ShelfView shelf, ObjectView obj)
        {
            shelf.Grid[obj.GridX, obj.LayerIndex] = obj;
            UpdateShelfVisuals(shelf, true);
        }

        public void UpdateShelfVisuals(ShelfView shelf, bool animate)
        {
            var currentActiveLayer = -1;
                
            // Find the active layer
            for (var layer = 0; layer < shelf.Data.LayerCount; layer++)
            {
                var hasItems = false;
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    if (shelf.Grid[x, layer] != null)
                    {
                        hasItems = true;
                        break;
                    }
                }
                
                if (hasItems)
                {
                    currentActiveLayer = layer;
                    shelf.CurrentFrontLayer = layer;
                    break;
                }
            }

            if (currentActiveLayer == -1) 
                return;

            var startX = -(shelf.Data.Width - 1) * ItemVisualWidth / 2f;

            for (var x = 0; x < shelf.Data.Width; x++)
            {
                for (var layer = 0; layer < shelf.ColumnMaxDepths[x]; layer++)
                {
                    var obj = shelf.Grid[x, layer];
                    if (obj == null) 
                        continue;
                    
                    var relativeDepth = layer - currentActiveLayer;

                    switch (relativeDepth)
                    {
                        case 0:
                            obj.SetState(ObjectState.Front);
                            break;
                        case 1:
                            obj.SetState(ObjectState.Back);
                            break;
                        default:
                            obj.SetState(ObjectState.Hidden);
                            break;
                    }

                    var visualDepth = Mathf.Min(relativeDepth, 2);
                    var posX = startX + (x * ItemVisualWidth);
                    var posY = ItemOffsetY + (visualDepth * LayerOffset.y);
                    
                    obj.MoveToLocalPosition(new Vector3(posX, posY, 0f), animate);
                }
            }
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