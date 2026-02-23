using UnityEngine;

namespace Level.Shelf
{
    public class ShelfView : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        
        public Transform ItemContainer; 
        
        public int ShelfIndex { get; private set; }
        public ShelfData Data { get; private set; }

        public void Init(int index, ShelfData data, float itemVisualWidth)
        {
            ShelfIndex = index;
            Data = data;

            // Adjust the background sprite width based on how many items it needs to hold.
            // We use itemVisualWidth + padding to ensure the shelf is wide enough.
            var targetWidth = (data.Width * itemVisualWidth) + 0.5f; 
            Renderer.size = new Vector2(targetWidth, Renderer.size.y);
        }
    }
}