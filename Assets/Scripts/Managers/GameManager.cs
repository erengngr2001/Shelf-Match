using Game;
using UnityEngine;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } 

        [Header("Core References")]
        public GamePools PoolsData;
        public LevelManager LevelManager;
        public InputManager InputManager;

        [Header("User Data")]
        public int CurrentLevel = 1;

        private void Awake()
        {
            if (Instance == null) 
                Instance = this;
            else 
                Destroy(gameObject);
        }

        private void Start()
        {
            PoolsData.Init();
            
            InputManager.OnObjectTapped += HandleObjectTapped;
            InputManager.OnObjectHeld += HandleObjectHeld;
            
            // todo: maybe add a main menu for levels?
            PlayCurrentLevel();
        }
        
        private void OnDestroy()
        {
            if (InputManager != null)
            {
                InputManager.OnObjectTapped -= HandleObjectTapped;
                InputManager.OnObjectHeld -= HandleObjectHeld;
            }
        }
        
        private void Update()
        {
            InputManager.ManualUpdate();
            LevelManager.ManualUpdate();
            // StackManager.ManualUpdate();
        }
        
        private void HandleObjectTapped(Level.Objects.ObjectView clickedObj)
        {
            Debug.Log($"[TAP] Extracted '{clickedObj.Id.Value}' from Shelf {clickedObj.ShelfIndex}");
            
            // 1. Remove from logical grid and uncover back layers
            LevelManager.ObjectManager.ExtractObject(clickedObj);
            
            // 2. TODO: Send to StackManager
            // StackManager.AddItem(clickedObj);
        }

        private void HandleObjectHeld(Level.Objects.ObjectView heldObj)
        {
            Debug.Log($"[HOLD] Player is inspecting '{heldObj.Id.Value}'");
            // You can use this later to trigger a wobble animation, show an info tooltip, etc.
        }

        private void PlayCurrentLevel()
        {
            LevelManager.StartLevel(CurrentLevel);
        }

        public void LevelCompleted()
        {
            CurrentLevel++;
            Debug.Log($"GameManager: Saving user progress. Advancing to Level {CurrentLevel}");
            
            // For this prototype, immediately loop into the next level
            // todo: maybe need a small win ui here before proceeding
            PlayCurrentLevel();
        }
    }
}