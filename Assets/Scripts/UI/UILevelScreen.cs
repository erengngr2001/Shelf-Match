using System;
using Managers;
using UnityEngine;

namespace UI
{
    public class UILevelScreen : MonoBehaviour
    {
        public UIButton UndoButton;
        public UIButton RestartButton;

        public UIWinScreen WinScreen;
        public UILoseScreen LoseScreen;

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

        public void ShowWinScreen()
        {
            WinScreen.Show();
        }

        public void ShowLoseScreen()
        {
            LoseScreen.Show();
        }

        public void CloseSubscreens()
        {
            WinScreen.HideSilent();
            LoseScreen.HideSilent();
        }
    }
}