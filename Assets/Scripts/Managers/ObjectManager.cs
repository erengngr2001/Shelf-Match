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
            public ShelfView Shelf;
            public int X;
            public int Layer;
        }

        [Header("Placement Settings")]
        public float ItemVisualWidth;
        
        private List<Sprite> _availableItemSprites = new List<Sprite>();
        private List<ShelfView> _activeShelves;

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
            _activeShelves = activeShelves;
            ClearItems();

            var totalSlots = 0;
            foreach (var shelf in activeShelves)
            {
                // shelf.SetupGrid() is already called by ShelfManager during Generation!
                totalSlots += shelf.Data.Width * shelf.Data.LayerCount;
            }

            var totalItemsToPlace = (totalSlots / 3) * 3;

            CalculateColumnDepths(activeShelves, totalItemsToPlace);

            for (var triplet = 0; triplet < totalItemsToPlace / 3; triplet++)
            {
                var randomSprite = _availableItemSprites[Random.Range(0, _availableItemSprites.Count)];
                var id = new ObjectId(randomSprite.name);

                for (var i = 0; i < 3; i++)
                {
                    using var _ = ListPool<ShelfSlotPointer>.Get(out var openSlots);
                    GetAvailableBackmostSlots(activeShelves, openSlots);
                    
                    if (openSlots.Count == 0) break;

                    var slot = openSlots[Random.Range(0, openSlots.Count)];
                    var obj = GamePools.Instance.ObjectViewPool.Get();
                    
                    obj.transform.SetParent(slot.Shelf.ItemContainer.transform, false);
                    obj.Init(id, randomSprite, slot.Shelf, slot.X, slot.Layer);

                    slot.Shelf.AddObject(obj, slot.X, slot.Layer);
                }
            }
        }

        private void CalculateColumnDepths(List<ShelfView> activeShelves, int totalItems)
        {
            foreach (var shelf in activeShelves)
                for (var x = 0; x < shelf.Data.Width; x++) 
                    shelf.SetColumnDepth(x, 0);

            var currentLayer = 0;
            var itemsLeft = totalItems;

            while (itemsLeft > 0)
            {
                foreach (var shelf in activeShelves)
                {
                    for (var x = 0; x < shelf.Data.Width && itemsLeft > 0; x++)
                    {
                        if (currentLayer < shelf.Data.LayerCount)
                        {
                            shelf.SetColumnDepth(x, shelf.GetColumnDepth(x) + 1);
                            itemsLeft--;
                        }
                    }
                }
                
                currentLayer++;
            }
        }

        private void GetAvailableBackmostSlots(List<ShelfView> activeShelves, List<ShelfSlotPointer> results)
        {
            results.Clear();
            
            foreach (var shelf in activeShelves)
            {
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    var maxDepth = shelf.GetColumnDepth(x);
                    for (var layer = maxDepth - 1; layer >= 0; layer--)
                    {
                        if (shelf.IsSlotEmpty(x, layer))
                        {
                            results.Add(new ShelfSlotPointer
                            {
                                Shelf = shelf, 
                                X = x, 
                                Layer = layer
                            });
                            
                            break; 
                        }
                    }
                }
            }
        }

        public void ClearItems()
        {
            foreach (var shelf in _activeShelves)
                shelf.ClearShelf(GamePools.Instance.ObjectViewPool);
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