using System;
using UnityEngine;

namespace UI
{
    public abstract class UISubscreen : MonoBehaviour
    {
        protected Action _onComplete;

        public virtual void Show(Action onComplete = null)
        {
            _onComplete = onComplete;
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            
            _onComplete?.Invoke();
            _onComplete = null;
        }

        // Instantly closes the screen without firing callbacks (useful for level resets)
        public virtual void HideSilent()
        {
            _onComplete = null;
            gameObject.SetActive(false);
        }
    }
}