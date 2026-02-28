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

        [Header("Cameras")]
        public Camera EnvironmentCamera;
        public Camera StackCamera;

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
            
            // todo: maybe add a main menu for levels?
            PlayCurrentLevel();
        }
        
        private void Update()
        {
            InputManager.ManualUpdate();
            LevelManager.ManualUpdate();
        }

        private void PlayCurrentLevel()
        {
            LevelManager.StartLevel(CurrentLevel);
        }

        public void LevelCompleted()
        {
            CurrentLevel++;
            
            // Manually cap max level to 3 for this prototype
            if (CurrentLevel > 3)
                CurrentLevel = 3;
            
            Debug.Log($"GameManager: Saving user progress. Advancing to Level {CurrentLevel}");
            
            // For this prototype, immediately loop into the next level
            // todo: maybe need a small win ui here before proceeding
            PlayCurrentLevel();
        }

        public Vector3 SwitchCameraSpace(Vector3 worldPos, Camera fromCam, Camera toCam)
        {
            Vector3 screenPos = fromCam.WorldToScreenPoint(worldPos);
            screenPos.z = Mathf.Abs(toCam.transform.position.z);
            Vector3 newWorldPos = toCam.ScreenToWorldPoint(screenPos);
            newWorldPos.z = 0f; 
            return newWorldPos;
        }
    }
}