using System;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Managers
{
    [Serializable]
    public struct InputCameraGroup
    {
        // The camera defining the world space for this layer group
        public Camera ReferenceCamera;
        // The layers this specific camera is allowed to interact with
        public LayerMask InteractableMask;
    }

    public class InputManager : MonoBehaviour, IManualUpdate
    {
        [Header("Input Evaluation Order (Top to Bottom)")]
        public InputCameraGroup[] InputGroups;
        
        [Header("Settings")]
        public float HoldDurationThreshold; 
        public float DragCancelThreshold; 
        
        private bool _isPointerDown;
        private float _pointerDownTimer;
        private Vector2 _pointerDownPosition;
        private bool _holdTriggered;
        private IInteractable _hoveredInteractable;
        
        private readonly float _touchRadius = 0.2f;

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

            // Iterate through our cameras in order of priority
            foreach (var group in InputGroups)
            {
                var worldPosition = group.ReferenceCamera.ScreenToWorldPoint(screenPosition);
                
                var hitCollider = Physics2D.OverlapCircle(worldPosition, _touchRadius, group.InteractableMask);
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
                        
                        _hoveredInteractable.InteractDown();
                        
                        // Return if you hit some target in priority 
                        // Prevents multiple interactions on a single click
                        return; 
                    }
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
            }
        }

        private void HandlePointerUp()
        {
            if (!_isPointerDown) 
                return;

            if (_hoveredInteractable != null)
            {
                _hoveredInteractable.InteractUp();
                
                if (!_holdTriggered)
                    _hoveredInteractable.InteractTapped();
            }

            ResetInputState();
        }

        private void CancelInput()
        {
            if (_hoveredInteractable != null)
                _hoveredInteractable.InteractCancel();
            
            ResetInputState();
        }

        private void ResetInputState()
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