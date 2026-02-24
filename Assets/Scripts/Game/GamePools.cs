using Level.Objects;
using Level.Shelf;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GamePools", menuName = "ShelfMatch/GamePools")]
    public class GamePools : ScriptableObject
    {
        public static GamePools Instance;
        
        [Header("Puzzle Elements")]
        public ObjectPool<ObjectView> ObjectViewPool;
        public ObjectPool<ShelfView> ShelfViewPool;
        
        // [Header("Visual Effects")]
        // Vfx here
        
        public void Init()
        {
            Instance = this;
            
            ObjectViewPool.Init();
            ShelfViewPool.Init();
        }

        private void OnDisable()
        {
            ObjectViewPool?.Dispose();
            ShelfViewPool?.Dispose();
        }
    }
}