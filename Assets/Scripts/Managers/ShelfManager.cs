using System.Collections.Generic;
using Game;
using Level.Objects;
using Level.Shelf;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public class ShelfManager : MonoBehaviour, IManualUpdate
    {
        [Header("Layout Settings")]
        public float ShelfSpacingY;
        public float ItemVisualWidth;
        public float MaxPlayAreaWidth;
        public float MaxPlayAreaHeight;

        [Header("Visual Settings")]
        public float ItemOffsetY;
        public Vector3 LayerOffset;

        public List<ShelfView> ActiveShelves { get; private set; } = new List<ShelfView>();
        
        private bool _isVisualsDirty;

        public void ManualUpdate()
        {
            if (!_isVisualsDirty)
                return;
            
            _isVisualsDirty = false;
            UpdateAllShelvesVisuals();
        }
        
        public void GenerateShelves(List<ShelfData> shelfDataList)
        {
            var totalShelves = shelfDataList.Count;
            var startY = (totalShelves - 1) * ShelfSpacingY / 2f;
            var maxActualWidth = 0f;

            for (var i = 0; i < totalShelves; i++)
            {
                var data = shelfDataList[i];
            
                var currentShelfWidth = data.Width * ItemVisualWidth;
                if (currentShelfWidth > maxActualWidth) maxActualWidth = currentShelfWidth;

                // y: +15f is there because the bottom of screen is the stack area
                // so, the shelves should be initialized not from middle of the screen but a bit higher
                var spawnPos = new Vector3(0, startY - (i * ShelfSpacingY) + 15f, 0);
                
                var newShelf = GamePools.Instance.ShelfViewPool.Get();
                newShelf.transform.localPosition = spawnPos;
            
                newShelf.Init(i, data, ItemVisualWidth);
                newShelf.SetupGrid();
                ActiveShelves.Add(newShelf);
            }

            var totalActualHeight = (totalShelves - 1) * ShelfSpacingY + 2f; 
            var scaleX = MaxPlayAreaWidth / maxActualWidth;
            var scaleY = MaxPlayAreaHeight / totalActualHeight;
            var finalScale = Mathf.Min(scaleX, scaleY, 1f);

            var gameManager = GameManager.Instance;

            gameManager.EnvironmentCamera.orthographicSize = CameraUtilities.BaselineCameraSize / finalScale;
        }

        public void ExtractObjectFromShelf(ShelfView shelf, ObjectView obj)
        {
            shelf.Grid[obj.GridX, obj.LayerIndex] = null;
            obj.SetState(ObjectState.Collected);
            
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

            for (var x = 0; x < shelf.Data.Width; x++)
            {
                for (var layer = 0; layer < shelf.ColumnMaxDepths[x]; layer++)
                {
                    var obj = shelf.Grid[x, layer];
                    
                    if (obj == null) 
                        continue;
                    
                    if (obj.State == ObjectState.Collected)
                        continue;
                    
                    var relativeDepth = layer - currentActiveLayer;

                    obj.SetStateByRelativeDepth(relativeDepth);

                    var targetWorldPos = GetWorldPositionForSlot(shelf, x, layer);
                    
                    obj.MoveToWorldPosition(targetWorldPos, animate);
                }
            }
        }
        
        public void UpdateAllShelvesVisuals()
        {
            foreach (var shelf in ActiveShelves)
                UpdateShelfVisuals(shelf, false);
        }

        public void Clear()
        {
            foreach (var shelf in ActiveShelves)
            {
                if (shelf != null) 
                    GamePools.Instance.ShelfViewPool.Release(shelf);
            }
            ActiveShelves.Clear();
        }
        
        #region UNDO
        
        public bool TryGetValidUndoSlot(ObjectView item, out ShelfSlotPointer result)
        {
            result = default;

            // Original Position is available
            if (item.ParentShelf.CurrentFrontLayer == item.LayerIndex && 
                item.ParentShelf.IsSlotEmpty(item.GridX, item.LayerIndex))
            {
                result = new ShelfSlotPointer
                {
                    Shelf = item.ParentShelf, 
                    X = item.GridX, 
                    Layer = item.LayerIndex
                };
                
                return true;
            }

            // First available empty slot on any active layer
            using var _ = ListPool<ShelfSlotPointer>.Get(out var availableSlots);

            foreach (var shelf in ActiveShelves)
            {
                var activeLayer = GetFrontmostActiveLayer(shelf);
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    // Ensure the slot exists within the column's valid depth bounds and is empty
                    if (activeLayer < shelf.ColumnMaxDepths[x] && 
                        shelf.IsSlotEmpty(x, activeLayer))
                    {
                        availableSlots.Add(new ShelfSlotPointer
                        {
                            Shelf = shelf, 
                            X = x, 
                            Layer = activeLayer
                        });
                    }
                }
            }

            if (availableSlots.Count > 0)
            {
                var randomIndex = Random.Range(0, availableSlots.Count);
                result = availableSlots[randomIndex];
                return true;
            }

            // No empty slots could be found, return false to ignore the undo action
            // todo: put a floating text object here so the user gets a feedback
            return false;
        }

        private int GetFrontmostActiveLayer(ShelfView shelf)
        {
            for (var layer = 0; layer < shelf.Data.LayerCount; layer++)
            {
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    if (layer < shelf.ColumnMaxDepths[x] && 
                        !shelf.IsSlotEmpty(x, layer))
                        return layer;
                }
            }
            // Fallback to the current front layer if the shelf happens to be completely cleared
            return shelf.CurrentFrontLayer; 
        }

        public void ReturnObjectToShelf(ObjectView item, ShelfSlotPointer slot)
        {
            var shelf = slot.Shelf;
            var x = slot.X;
            var layer = slot.Layer;
    
            shelf.AddObject(item, x, layer);

            item.Init(item.Id, item.Renderer.sprite, shelf, x, layer);
            item.SetState(ObjectState.Collected);
    
            var targetWorldPos = GetWorldPositionForSlot(shelf, x, layer); 

            item.transform.position = CameraUtilities.SwitchCameraSpace(
                item.transform.position, 
                GameManager.Instance.StackCamera, 
                GameManager.Instance.EnvironmentCamera
            );
            item.gameObject.layer = LayerMask.NameToLayer("Interactable");

            Tween.Position(item.transform, targetWorldPos, 0.3f, Ease.OutQuad)
                // todo: allocates
                .OnComplete(this, (manager) => {
                item.SetState(ObjectState.None);
                manager._isVisualsDirty = true; 
            });

            UpdateShelfVisuals(shelf, true);
    
        }
        
        #endregion

        private Vector3 GetWorldPositionForSlot(ShelfView shelf, int x, int layer)
        {
            var currentActiveLayer = GetFrontmostActiveLayer(shelf);
            var startX = -(shelf.Data.Width - 1) * ItemVisualWidth / 2f;
            var relativeDepth = layer - currentActiveLayer;
            var visualDepth = Mathf.Clamp(relativeDepth, 0, 2);

            var localPosX = startX + (x * ItemVisualWidth);
            var localPosY = ItemOffsetY + (visualDepth * LayerOffset.y);

            return shelf.transform.TransformPoint(new Vector3(localPosX, localPosY, 0f));
        }
    } 
}