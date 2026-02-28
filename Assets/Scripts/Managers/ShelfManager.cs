using System.Collections.Generic;
using Game;
using Level.Objects;
using Level.Shelf;
using PrimeTween;
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
            if (item.ParentShelf.IsSlotEmpty(item.GridX, item.LayerIndex))
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
            foreach (var shelf in ActiveShelves)
            {
                var activeLayer = GetFrontmostActiveLayer(shelf);
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    if (shelf.IsSlotEmpty(x, activeLayer))
                    {
                        result = new ShelfSlotPointer
                        {
                            Shelf = shelf, 
                            X = x, 
                            Layer = activeLayer
                        };
                        
                        return true;
                    }
                }
            }

            return false;
        }

        // todo: ensure this works correctly, try edge cases
        private int GetFrontmostActiveLayer(ShelfView shelf)
        {
            for (var layer = 0; layer < shelf.Data.LayerCount; layer++)
            {
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    if (!shelf.IsSlotEmpty(x, layer))
                        return layer;
                }
            }
            return 0; 
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
        
        private Vector3 GetLocalPositionForSlot(ShelfView shelf, int x, int layer)
        {
            var currentActiveLayer = GetFrontmostActiveLayer(shelf);
            var startX = -(shelf.Data.Width - 1) * ItemVisualWidth / 2f;
    
            var relativeDepth = layer - currentActiveLayer;
    
            var visualDepth = Mathf.Clamp(relativeDepth, 0, 2);
    
            var posX = startX + (x * ItemVisualWidth);
            var posY = ItemOffsetY + (visualDepth * LayerOffset.y);
    
            return new Vector3(posX, posY, 0f);
        }
        
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