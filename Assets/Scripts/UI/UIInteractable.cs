using Game;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(Collider2D))]
    public class UIInteractable : UIView, IInteractable
    {
        public Collider2D Collider;

        public virtual bool CanInteract => IsVisible();

        public void InteractDown() => OnInputDown();
        public void InteractHeld() => OnInputHeld();
        public void InteractUp() => OnInputUp();
        public void InteractTapped() => OnInputClick();
        public void InteractCancel() => OnInputCancel();

        protected virtual void OnInputDown() { }
        protected virtual void OnInputHeld() { }
        protected virtual void OnInputUp() { }
        protected virtual void OnInputClick() { }
        protected virtual void OnInputCancel() { }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Collider = GetComponent<Collider2D>();
            if (Collider != null) 
                Collider.isTrigger = true;
        }
#endif
    }
}