using System;

namespace Level.Shelf
{
    [Serializable]
    public struct ShelfSlotPointer
    {
        public ShelfView Shelf;
        public int X;
        public int Layer;
    }
}