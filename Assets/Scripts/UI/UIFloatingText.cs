using PrimeTween;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIFloatingText : MonoBehaviour
    {
        private static UIFloatingText _instance;

        public TextMeshPro Text; // Note: This uses the 3D TextMeshPro, not TextMeshProUGUI
        
        private Sequence _tween;
        
        private void Awake()
        {
            _instance = this;
            Text.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        private void _Show(string text, float posY)
        {
            if (_tween.isAlive)
                _tween.Stop();

            Text.SetText(text);
            Text.gameObject.SetActive(true);
            
            var transform = Text.transform;

            Text.alpha = 1f;
            transform.localScale = Vector3.zero;
            transform.localPosition = new Vector3(0f, posY, 0f);

            var scale = Tween.Scale(transform, Vector3.one, 0.2f);
            var move = Tween.LocalPositionY(transform, posY + 3f, 1f);
            var alpha = Tween
                .Alpha(Text, 0f, 0.5f, startDelay: 1f)
                .OnComplete(Text, t => { 
                    t.gameObject.SetActive(false);
                });

            _tween = Sequence
                .Create()
                .Group(scale)
                .Group(move)
                .Group(alpha);
        }

        public static void Show(string text)
        {
            if (_instance != null)
                _instance._Show(text, -2f); // Starting Y position for the floating text
        }
    }
}