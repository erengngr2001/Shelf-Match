using System;
using Managers;
using PrimeTween;

namespace UI
{
    public class UIWinScreen : UISubscreen
    {
        private const float SCREEN_CLOSE_DELAY = 2.5f;
        
        public override void Show(Action onComplete = null)
        {
            base.Show(onComplete);
            
            Tween.Delay(SCREEN_CLOSE_DELAY)
                .OnComplete(this, screen => {
                screen.Hide();
                GameManager.Instance.LevelCompleted();
            });
        }
    } 
}