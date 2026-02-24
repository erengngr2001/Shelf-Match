using UnityEngine;

namespace Managers
{
    public enum LevelState
    {
        Initializing,
        Playing,
        Win,
        Fail
    }

    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("References")]
        public ShelfManager ShelfManager;
        
        public LevelState State { get; private set; }
        
        private int _currentLevelNumber;

        private void Awake()
        {
            if (Instance == null) 
                Instance = this;
            else 
                Destroy(gameObject);
        }

        public void StartLevel(int levelNumber)
        {
            State = LevelState.Initializing;
            _currentLevelNumber = levelNumber;

            ShelfManager.LoadLevel(_currentLevelNumber);
            
            State = LevelState.Playing;
        }

        public void RestartLevel()
        {
            if (State == LevelState.Initializing) 
                return;
            
            Debug.Log($"LevelManager: Restarting Level {_currentLevelNumber}...");
            StartLevel(_currentLevelNumber);
        }

        public void CheckWinCondition(int totalItemsRemainingInLevel)
        {
            if (State != LevelState.Playing) 
                return;

            if (totalItemsRemainingInLevel <= 0)
            {
                State = LevelState.Win;
                Debug.Log("LevelManager: WIN STATE! Level Cleared.");
                
                GameManager.Instance.LevelCompleted();
            }
        }

        public void TriggerFailState()
        {
            if (State != LevelState.Playing) 
                return;

            State = LevelState.Fail;
            Debug.Log("LevelManager: FAIL STATE! Stack is full.");
            // todo: Trigger Fail UI / Prompt Restart
        }
    }
}