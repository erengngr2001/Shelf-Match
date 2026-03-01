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
        private List<Sprite> _availableItemSprites = new List<Sprite>();

        private void Awake()
        {
            LoadItemSprites();
        }
        
        public void ManualUpdate()
        {
            // Frame-by-frame object logic (animations etc.) will go here
        }
        
        public int GenerateItems(List<ShelfView> activeShelves)
        {
            var totalSlots = 0;
            foreach (var shelf in activeShelves)
                totalSlots += shelf.Data.Width * shelf.Data.LayerCount;

            // If the total shelf space is not a multiple of 3,
            // we reduce total item count to a multiple of 3
            var totalItemsToPlace = (totalSlots / 3) * 3;

            CalculateColumnDepths(activeShelves, totalItemsToPlace);

            using var h1 = ListPool<ShelfSlotPointer>.Get(out var allSlots);
            foreach (var shelf in activeShelves)
            {
                for (var x = 0; x < shelf.Data.Width; x++)
                {
                    var maxDepth = shelf.GetColumnDepth(x);
                    for (var layer = 0; layer < maxDepth; layer++)
                        allSlots.Add(new ShelfSlotPointer
                        {
                            Shelf = shelf, 
                            X = x, 
                            Layer = layer
                        });
                }
            }

            using var h2 = GenericPool<SortedDictionary<int, List<ShelfSlotPointer>>>.Get(out var slotsByLayer);
            slotsByLayer.Clear();
            foreach (var slot in allSlots)
            {
                if (!slotsByLayer.TryGetValue(slot.Layer, out var layerList))
                {
                    layerList = ListPool<ShelfSlotPointer>.Get();
                    slotsByLayer[slot.Layer] = layerList;
                }
        
                layerList.Add(slot);
            }

            using var h3 = ListPool<ShelfSlotPointer>.Get(out var orderedSlots);
            foreach (var kvp in slotsByLayer)
            {
                var layerSlots = kvp.Value;
                ShuffleList(layerSlots);
                orderedSlots.AddRange(layerSlots);
        
                ListPool<ShelfSlotPointer>.Release(layerSlots);
            }

            var totalTriplets = totalItemsToPlace / 3;
            using var h4 = ListPool<Sprite>.Get(out var tripletDeck);
            
            // Object placement is still randomized but the total number of created objects for each type
            // is not completely randomized. This prevents levels consisting of too much repetition of a 
            // single object type. It does not harm randomization but makes it feel better overall.
            // Result: You get an even amount of different objects
            for (var i = 0; i < totalTriplets; i++)
                tripletDeck.Add(_availableItemSprites[i % _availableItemSprites.Count]);

            ShuffleList(tripletDeck);

            for (var i = 0; i < totalTriplets; i++)
            {
                var selectedSprite = tripletDeck[i];
                var id = new ObjectId(selectedSprite.name);

                for (var j = 0; j < 3; j++)
                {
                    var slot = orderedSlots[(i * 3) + j];
                    
                    var obj = GamePools.Instance.ObjectViewPool.Get();
                    obj.gameObject.layer = LayerMask.NameToLayer("Interactable");
                    obj.transform.localScale = obj.DefaultScale;
                    
                    obj.Init(id, selectedSprite, slot.Shelf, slot.X, slot.Layer);
                    slot.Shelf.AddObject(obj, slot.X, slot.Layer);
                }
            }
            
            return totalItemsToPlace;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var temp = list[i];
                var randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
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

        public void Clear(List<ShelfView> activeShelves)
        {
            foreach (var shelf in activeShelves)
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