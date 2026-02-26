using PrimeTween;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    public class UIButton : UIInteractable
    {
        [Header("Events")]
        public UnityEvent OnClick;

        private float PressedScaleMultiplier = 0.90f;
        private float AnimationDuration = 0.1f;

        private bool _scaleModified;
        private Vector3 _cachedScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _cachedScale = _transform.localScale;
        }

        protected override void OnInputClick()
        {
            OnClick?.Invoke();
        }

        protected override void OnInputDown()
        {
            _scaleModified = true;
            _scaleTween.Stop();
            
            _scaleTween = Tween.Scale(_transform, _cachedScale * PressedScaleMultiplier, AnimationDuration, Ease.OutQuad);
        }

        protected override void OnInputUp() => ResetScale();
        protected override void OnInputCancel() => ResetScale();

        private void ResetScale()
        {
            if (_scaleModified)
            {
                _scaleModified = false;
                _scaleTween.Stop();
                
                _scaleTween = Tween.Scale(_transform, _cachedScale, AnimationDuration, Ease.OutBack);
            }
        }
    }
}