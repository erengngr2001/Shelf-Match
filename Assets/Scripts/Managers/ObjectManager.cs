using System.Collections.Generic;
using Game;
using Level.Objects;
using Level.Shelf;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public class ObjectManager : MonoBehaviour, IManualUpdate
    {
        private struct ShelfSlotPointer
        {
            public int ShelfIndex;
            public int X;
            public int Layer;
        }

        [Header("Placement Settings")]
        public float ItemVisualWidth;
        public float ItemOffsetY;
        public Vector3 LayerOffset;

        private const int MAX_SHELVES = 10;
        private const int MAX_WIDTH = 10;
        private const int MAX_LAYERS = 8;

        private readonly ObjectView[,,] _board = new ObjectView[MAX_SHELVES, MAX_WIDTH, MAX_LAYERS];
        private readonly int[,] _columnMaxDepths = new int[MAX_SHELVES, MAX_WIDTH];
        
        private int _activeShelfCount;
        private readonly int[] _activeShelfWidths = new int[MAX_SHELVES];

        private List<Sprite> _availableItemSprites = new List<Sprite>();

        private void Awake()
        {
            LoadItemSprites();
        }
        
        public void ManualUpdate()
        {
            // Frame-by-frame object logic (animations etc.) will go here
        }
        
        public void GenerateItems(List<ShelfData> shelfDataList, List<ShelfView> activeShelves)
        {
            ClearItems();

            _activeShelfCount = shelfDataList.Count;
            var totalSlots = 0;

            for (var i = 0; i < _activeShelfCount; i++)
            {
                var data = shelfDataList[i];
                
                _activeShelfWidths[i] = data.Width;
                
                totalSlots += data.Width * data.LayerCount;
            }

            var totalTriplets = totalSlots / 3;
            var totalItemsToPlace = totalTriplets * 3;

            CalculateColumnDepths(shelfDataList, totalItemsToPlace);

            for (var triplet = 0; triplet < totalTriplets; triplet++)
            {
                var randomSprite = _availableItemSprites[Random.Range(0, _availableItemSprites.Count)];
                var id = new ObjectId(randomSprite.name);

                for (var i = 0; i < 3; i++)
                {
                    using var _ = ListPool<ShelfSlotPointer>.Get(out var openSlots);
                    GetAvailableBackmostSlots(openSlots);
                    
                    var slot = openSlots[Random.Range(0, openSlots.Count)];
                    var obj = GamePools.Instance.ObjectViewPool.Get();
                    
                    var parentShelf = activeShelves[slot.ShelfIndex];
                    obj.transform.SetParent(parentShelf.ItemContainer.transform, false);
                    
                    var startX = -(shelfDataList[slot.ShelfIndex].Width - 1) * ItemVisualWidth / 2f;
                    var posX = startX + (slot.X * ItemVisualWidth);
                    var posY = ItemOffsetY + (slot.Layer * LayerOffset.y);
                    
                    var finalLocalPos = new Vector3(posX, posY, 0f);
                    
                    obj.Init(id, randomSprite, slot.ShelfIndex, slot.X, slot.Layer);
                    obj.Renderer.sortingOrder = -slot.Layer;

                    _board[slot.ShelfIndex, slot.X, slot.Layer] = obj;
                }
            }
            
            UpdateBoardVisuals(false);
        }

        private void CalculateColumnDepths(List<ShelfData> shelfDataList, int totalItems)
        {
            for (var shelf = 0; shelf < _activeShelfCount; shelf++)
            for (var x = 0; x < _activeShelfWidths[shelf]; x++)
            {
                _columnMaxDepths[shelf, x] = 0;
            }

            var currentLayer = 0;
            var itemsLeft = totalItems;

            while (itemsLeft > 0)
            {
                for (var shelf = 0; shelf < _activeShelfCount; shelf++)
                for (var x = 0; x < _activeShelfWidths[shelf] && itemsLeft > 0; x++)
                {
                    if (currentLayer < shelfDataList[shelf].LayerCount)
                    {
                        _columnMaxDepths[shelf, x]++;
                        itemsLeft--;
                    }
                }
                
                currentLayer++;
            }
        }

        private void GetAvailableBackmostSlots(List<ShelfSlotPointer> results)
        {
            results.Clear();
            
            for (var shelf = 0; shelf < _activeShelfCount; shelf++)
            {
                for (var x = 0; x < _activeShelfWidths[shelf]; x++)
                {
                    var maxDepth = _columnMaxDepths[shelf, x];
                    
                    for (var layer = maxDepth - 1; layer >= 0; layer--)
                    {
                        if (_board[shelf, x, layer] == null)
                        {
                            results.Add(new ShelfSlotPointer
                            {
                                ShelfIndex = shelf, 
                                X = x, 
                                Layer = layer
                            });
                            
                            break; 
                        }
                    }
                }
            }
        }

        // public void UpdateBoardVisuals()
        // {
        //     // Loop through each shelf individually
        //     for (var shelf = 0; shelf < _activeShelfCount; shelf++)
        //     {
        //         var currentActiveLayer = -1;
        //         
        //         for (var layer = 0; layer < MAX_LAYERS; layer++)
        //         {
        //             var hasItems = false;
        //             
        //             for (var x = 0; x < _activeShelfWidths[shelf]; x++)
        //             {
        //                 if (_board[shelf, x, layer] != null)
        //                 {
        //                     hasItems = true;
        //                     break;
        //                 }
        //             }
        //             
        //             // If we found an item, this layer is the front-most for this shelf
        //             if (hasItems)
        //             {
        //                 currentActiveLayer = layer;
        //                 break;
        //             }
        //         }
        //
        //         // If the shelf has no items left at all, skip visual updates for it
        //         if (currentActiveLayer == -1) 
        //             continue;
        //
        //         for (var x = 0; x < _activeShelfWidths[shelf]; x++)
        //         {
        //             var maxDepth = _columnMaxDepths[shelf, x];
        //             
        //             for (var layer = 0; layer < maxDepth; layer++)
        //             {
        //                 var obj = _board[shelf, x, layer];
        //                 
        //                 if (obj == null)
        //                     continue;
        //                 
        //                 // Apply the state dynamically
        //                 if (layer == currentActiveLayer) 
        //                     obj.SetState(ObjectState.Front);
        //                 else if (layer == currentActiveLayer + 1) 
        //                     obj.SetState(ObjectState.Back);
        //                 else 
        //                     obj.SetState(ObjectState.Hidden);
        //             }
        //         }
        //     }
        // }
        
        public void UpdateBoardVisuals(bool animate = true)
        {
            for (var shelf = 0; shelf < _activeShelfCount; shelf++)
            {
                var currentActiveLayer = -1;
                
                for (var layer = 0; layer < MAX_LAYERS; layer++)
                {
                    var hasItems = false;
                    for (var x = 0; x < _activeShelfWidths[shelf]; x++)
                    {
                        if (_board[shelf, x, layer] != null)
                        {
                            hasItems = true;
                            break;
                        }
                    }
                    
                    if (hasItems)
                    {
                        currentActiveLayer = layer;
                        break;
                    }
                }

                if (currentActiveLayer == -1) 
                    continue;

                // Calculate the true center X for this specific shelf layout
                var startX = -(_activeShelfWidths[shelf] - 1) * ItemVisualWidth / 2f;

                for (var x = 0; x < _activeShelfWidths[shelf]; x++)
                {
                    var maxDepth = _columnMaxDepths[shelf, x];
                    
                    for (var layer = 0; layer < maxDepth; layer++)
                    {
                        var obj = _board[shelf, x, layer];
                        if (obj == null)
                            continue;
                        
                        var relativeDepth = layer - currentActiveLayer;
                        
                        // 1. Assign Visual/Interactive States
                        if (relativeDepth == 0) 
                            obj.SetState(ObjectState.Front);
                        else if (relativeDepth == 1) 
                            obj.SetState(ObjectState.Back);
                        else 
                            obj.SetState(ObjectState.Hidden);

                        // 2. Calculate offset positions (Capping relative depth at 2)
                        // This guarantees all Hidden objects share the exact same physical depth as the Back object
                        var visualDepth = Mathf.Min(relativeDepth, 2);
                        
                        var posX = startX + (x * ItemVisualWidth);
                        var posY = ItemOffsetY + (visualDepth * LayerOffset.y);
                        
                        var targetPos = new Vector3(posX, posY, 0f);

                        // 3. Move the object
                        obj.MoveToLocalPosition(targetPos, animate);
                    }
                }
            }
        }

        public void ExtractObject(ObjectView targetObj)
        {
            var shelf = targetObj.ShelfIndex;
            var x = targetObj.GridX;
            var layer = targetObj.LayerIndex;

            _board[shelf, x, layer] = null;
            
            targetObj.SetState(ObjectState.MovingToStack);
            
            UpdateBoardVisuals();
        }

        public void UndoObjectPlacement(ObjectView returningObj)
        {
            var shelf = returningObj.ShelfIndex;
            var x = returningObj.GridX;
            var layer = returningObj.LayerIndex;

            _board[shelf, x, layer] = returningObj;
            returningObj.transform.localPosition = returningObj.OriginalShelfPosition;
            
            UpdateBoardVisuals();
        }

        public int GetTotalObjectsOnShelf(int shelfIndex)
        {
            var count = 0;
            
            for (var x = 0; x < _activeShelfWidths[shelfIndex]; x++)
            for (var layer = 0; layer < _columnMaxDepths[shelfIndex, x]; layer++)
            {
                if (_board[shelfIndex, x, layer] != null) 
                    count++;
            }
            
            return count;
        }

        public void ClearItems()
        {
            for (var shelf = 0; shelf < _activeShelfCount; shelf++)
            for (var x = 0; x < _activeShelfWidths[shelf]; x++)
            {
                var maxDepth = _columnMaxDepths[shelf, x];
                for (var layer = 0; layer < maxDepth; layer++)
                {
                    if (_board[shelf, x, layer] == null)
                        continue;
                    
                    GamePools.Instance.ObjectViewPool.Release(_board[shelf, x, layer]);
                    _board[shelf, x, layer] = null;
                }
            }
        }

        private void LoadItemSprites()
        {
            if (_availableItemSprites.Count > 0) 
                return;
            
            var allSprites = Resources.LoadAll<Sprite>("Sprites/Objects");
            foreach (var s in allSprites) 
                _availableItemSprites.Add(s);
        }
    }
}