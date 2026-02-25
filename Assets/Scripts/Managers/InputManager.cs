using System;
using Game;
using Level.Objects;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Managers
{
    public class InputManager : MonoBehaviour, IManualUpdate
    {
        public LayerMask InteractableLayerMask;
        
        [Tooltip("How long in seconds before a tap becomes a hold")]
        public float HoldDurationThreshold; 
        
        [Tooltip("If the finger moves this many pixels, cancel the input (swipe/drag)")]
        public float DragCancelThreshold; 

        public event Action<ObjectView> OnObjectTapped;
        public event Action<ObjectView> OnObjectHeld;

        public Camera MainCamera;
        
        // State Machine Trackers
        private bool _isPointerDown;
        private float _pointerDownTimer;
        private Vector2 _pointerDownPosition;
        private bool _holdTriggered;
        private ObjectView _hoveredObject;

        public void ManualUpdate()
        {
            if (GameManager.Instance.LevelManager.State != LevelState.Playing)
                return;
            
            if (Input.GetMouseButtonDown(0))
            {
                HandlePointerDown(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                HandlePointerHeld(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandlePointerUp();
            }
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            if (IsPointerOverUI()) 
                return;

            var worldPosition = MainCamera.ScreenToWorldPoint(screenPosition);
            
            var hitCollider = Physics2D.OverlapPoint(worldPosition, InteractableLayerMask);

            if (hitCollider != null && 
                hitCollider.TryGetComponent<ObjectView>(out var clickedObject))
            {
                if (clickedObject.IsInteractable)
                {
                    _isPointerDown = true;
                    _holdTriggered = false;
                    _pointerDownTimer = 0f;
                    _pointerDownPosition = screenPosition;
                    _hoveredObject = clickedObject;
                }
            }
        }

        private void HandlePointerHeld(Vector2 currentScreenPosition)
        {
            if (!_isPointerDown || 
                _hoveredObject == null) 
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
                
                OnObjectHeld?.Invoke(_hoveredObject);
                
                _hoveredObject = null; 
            }
        }

        private void HandlePointerUp()
        {
            if (!_isPointerDown) 
                return;

            // If the finger came up, and we didn't hold long enough, it's a tap
            if (!_holdTriggered && 
                _hoveredObject != null)
                OnObjectTapped?.Invoke(_hoveredObject);

            CancelInput();
        }

        private void CancelInput()
        {
            _isPointerDown = false;
            _hoveredObject = null;
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