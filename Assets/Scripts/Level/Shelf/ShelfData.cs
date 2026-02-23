namespace Level.Shelf
{
    public struct ShelfData
    {
        public int Width;
        public int LayerCount;

        public ShelfData(int width, int layerCount)
        {
            Width = width;
            LayerCount = layerCount;
        }
    }
}