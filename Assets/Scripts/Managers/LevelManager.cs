using System;
using System.Collections.Generic;
using Game;
using Level.Objects;
using Level.Shelf;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public enum LevelState
    {
        Initializing,
        Playing,
        Win,
        Fail
    }

    public class LevelManager : MonoBehaviour, IManualUpdate
    {
        public static LevelManager Instance { get; private set; }

        [Header("References")]
        public ShelfManager ShelfManager;
        public ObjectManager ObjectManager;
        
        public LevelState State { get; private set; }
        
        private int _currentLevelNumber;

        private void Awake()
        {
            if (Instance == null) 
                Instance = this;
            else 
                Destroy(gameObject);
        }
        
        private void OnEnable()
        {
            ObjectView.OnTapped += HandleObjectTapped;
            ObjectView.OnHeld += HandleObjectHeld;
        }

        private void OnDisable()
        {
            ObjectView.OnTapped -= HandleObjectTapped;
            ObjectView.OnHeld -= HandleObjectHeld;
        }
        
        public void ManualUpdate()
        {
            if (State != LevelState.Playing)
                return;

            ShelfManager.ManualUpdate();
            ObjectManager.ManualUpdate();
        }
        
        // Parse the text (e.g., "[3,2],[3,2],[3,2]")
        private void ParseLevelData(string rawText, List<ShelfData> dataList)
        {
            var cleanText = rawText.Trim().Replace(" ", "");
            var shelfStrings = cleanText.Split(new[] { "],[" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in shelfStrings)
            {
                var cleanPair = s.Replace("[", "").Replace("]", "");
                var values = cleanPair.Split(',');

                if (values.Length == 2 && 
                    int.TryParse(values[0], out int width) && 
                    int.TryParse(values[1], out int layers))
                {
                    dataList.Add(new ShelfData(width, layers));
                }
            }
        }

        public void StartLevel(int levelNumber)
        {
            State = LevelState.Initializing;
            _currentLevelNumber = levelNumber;

            var levelFile = Resources.Load<TextAsset>($"Levels/level{levelNumber}");
            if (levelFile == null)
            {
                Debug.LogError($"level{levelNumber} not found!");
                return;
            }

            using var _ = ListPool<ShelfData>.Get(out var parsedData);
            ParseLevelData(levelFile.text, parsedData);
            
            ShelfManager.GenerateShelves(parsedData);
            ObjectManager.GenerateItems(parsedData, ShelfManager.ActiveShelves);
            ShelfManager.UpdateAllShelvesVisuals();
            
            State = LevelState.Playing;
        }
        
        private void HandleObjectTapped(ObjectView clickedObj)
        {
            if (State != LevelState.Playing) 
                return;
            
            Debug.Log($"[TAP] Extracted '{clickedObj.Id.Value}' from Shelf {clickedObj.ParentShelf.ShelfIndex}");
            
            ShelfManager.ExtractObjectFromShelf(clickedObj.ParentShelf, clickedObj);
            // StackManager.AddItem(clickedObj);
        }

        private void HandleObjectHeld(ObjectView heldObj)
        {
            if (State != LevelState.Playing) 
                return;
            
            Debug.Log($"[HOLD] Player is inspecting '{heldObj.Id.Value}'");
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