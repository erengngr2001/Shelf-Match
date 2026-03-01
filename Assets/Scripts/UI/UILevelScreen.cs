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

        public Transform[] StackSlots;

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
    }
}