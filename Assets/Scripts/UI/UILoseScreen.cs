using Managers;

namespace UI
{
    public class UILoseScreen : UISubscreen
    {
        public UIButton RetryButton;
        
        private void OnEnable()
        {
            RetryButton.OnClick += OnRetryClicked;
        }

        private void OnDisable()
        {
            RetryButton.OnClick -= OnRetryClicked;
        }

        private void OnRetryClicked()
        {
            LevelManager.Instance.RestartLevel();
            Hide();
        }
    }
}