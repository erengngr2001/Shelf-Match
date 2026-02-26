using UnityEngine;

namespace UI
{
    public class UIView : MonoBehaviour
    {
        private Transform __transform;
        public Transform _transform
        {
            get
            {
                if (__transform == null)
                    __transform = transform;
                
                return __transform;
            }
        }

        public bool IsVisible() => gameObject.activeSelf;

        public void Show()
        {
            if (!IsVisible()) 
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (IsVisible()) 
                gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible) 
                Show();
            else 
                Hide();
        }
    }
}