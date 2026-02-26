using System;
using Managers;
using UnityEngine;

namespace UI
{
    public class UILevelScreen : MonoBehaviour
    {
        public SpriteRenderer TopPanel;    
        public SpriteRenderer StackPanel; 
        
        public UIButton UndoButton;
        public UIButton RestartButton;

        public Transform[] StackSlotTransforms;

        private void OnEnable()
        {
            UndoButton.OnClick += OnUndoClicked;
            RestartButton.OnClick += OnRestartClicked;
        }
        
        private void OnDisable()
        {
            UndoButton.OnClick -= OnUndoClicked;
            RestartButton.OnClick -= OnRestartClicked;
        }

        private void OnUndoClicked()
        {
            LevelManager.Instance.UndoLastMove();
        }

        private void OnRestartClicked()
        {
            LevelManager.Instance.RestartLevel();
        }
        
        /// <summary>
        /// Called by the StackManager to find where an item should physically move
        /// </summary>
        public Transform GetStackSlotTransform(int index)
        {
            if (index < 0 || index >= StackSlotTransforms.Length)
            {
                Debug.LogWarning($"Stack slot index {index} is out of bounds!");
                return null;
            }
            
            return StackSlotTransforms[index];
        }
    }
}