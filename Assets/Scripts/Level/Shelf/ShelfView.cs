using Level.Objects;
using UnityEngine;

namespace Level.Shelf
{
    public class ShelfView : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        
        public Transform ItemContainer; 
        
        public int ShelfIndex { get; private set; }
        public ShelfData Data { get; private set; }

        public ObjectView[,] Grid { get; private set; }
        public int[] ColumnMaxDepths { get; private set; }

        public int CurrentFrontLayer;

        public void Init(int index, ShelfData data, float itemVisualWidth)
        {
            ShelfIndex = index;
            Data = data;
            CurrentFrontLayer = 0;

            // Adjust the background sprite width based on how many items it needs to hold.
            // We use itemVisualWidth + padding to ensure the shelf is wide enough.
            var targetWidth = (data.Width * itemVisualWidth) + 0.5f; 
            Renderer.size = new Vector2(targetWidth, Renderer.size.y);
        }

        public void SetupGrid()
        {
            // todo: allocates
            Grid = new ObjectView[Data.Width, Data.LayerCount];
            ColumnMaxDepths = new int[Data.Width];
        }

        public void SetColumnDepth(int x, int depth) => ColumnMaxDepths[x] = depth;
        public int GetColumnDepth(int x) => ColumnMaxDepths[x];
        
        public bool IsSlotEmpty(int x, int layer) => Grid[x, layer] == null;

        public void AddObject(ObjectView obj, int x, int layer)
        {
            Grid[x, layer] = obj;
        }

        public void ClearShelf(Game.ObjectPool<ObjectView> pool)
        {
            for (var x = 0; x < Data.Width; x++)
            for (var layer = 0; layer < ColumnMaxDepths[x]; layer++)
            {
                if (Grid[x, layer] != null)
                {
                    pool.Release(Grid[x, layer]);
                    Grid[x, layer] = null;
                }
            }
        }
    }
}