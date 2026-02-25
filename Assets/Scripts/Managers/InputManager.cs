using Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Managers
{
    public class InputManager : MonoBehaviour, IManualUpdate
    {
        public LayerMask InteractableLayerMask;
        public float HoldDurationThreshold; 
        public float DragCancelThreshold; 
        public Camera MainCamera;
        
        private bool _isPointerDown;
        private float _pointerDownTimer;
        private Vector2 _pointerDownPosition;
        private bool _holdTriggered;
        private IInteractable _hoveredInteractable;

        public void ManualUpdate()
        {
            if (Input.GetMouseButtonDown(0))
                HandlePointerDown(Input.mousePosition);
            else if (Input.GetMouseButton(0))
                HandlePointerHeld(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0))
                HandlePointerUp();
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            if (IsPointerOverUI()) 
                return;

            var worldPosition = MainCamera.ScreenToWorldPoint(screenPosition);
            
            var hitCollider = Physics2D.OverlapPoint(worldPosition, InteractableLayerMask);

            if (hitCollider != null && 
                hitCollider.TryGetComponent<IInteractable>(out var interactable))
            {
                if (interactable.CanInteract)
                {
                    _isPointerDown = true;
                    _holdTriggered = false;
                    _pointerDownTimer = 0f;
                    _pointerDownPosition = screenPosition;
                    _hoveredInteractable = interactable;
                }
            }
        }

        private void HandlePointerHeld(Vector2 currentScreenPosition)
        {
            if (!_isPointerDown || 
                _hoveredInteractable == null) 
                return;

            // If the user drags their finger too far, they are probably trying to swipe, so cancel the click
            if (Vector2.Distance(_pointerDownPosition, currentScreenPosition) > DragCancelThreshold)
            {
                CancelInput();
                return;
            }

            _pointerDownTimer += Time.deltaTime;

            // If we cross the time threshold and haven't triggered yet, it's a hold
            if (_pointerDownTimer >= HoldDurationThreshold && 
                !_holdTriggered)
            {
                _holdTriggered = true;
                _hoveredInteractable.InteractHeld();
                _hoveredInteractable = null; 
            }
        }

        private void HandlePointerUp()
        {
            if (!_isPointerDown) 
                return;

            // If the finger came up, and we didn't hold long enough, it's a tap
            if (!_holdTriggered && 
                _hoveredInteractable != null)
                _hoveredInteractable.InteractTapped();

            CancelInput();
        }

        private void CancelInput()
        {
            _isPointerDown = false;
            _hoveredInteractable = null;
            _holdTriggered = false;
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) 
                return false;
            
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}