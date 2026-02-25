using System;
using Game;
using Level.Shelf;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;

namespace Level.Objects
{
    public enum ObjectState
    {
        None,
        Front,
        Back,
        Hidden,
        Collected
    }
    
    public class ObjectView : MonoBehaviour, IPoolable, IInteractable
    {
        // Global events for the LevelManager to listen to
        public static event Action<ObjectView> OnTapped;
        public static event Action<ObjectView> OnHeld;
        
        public SpriteRenderer Renderer;
        public PolygonCollider2D Collider;
        
        public ObjectState State { get; private set; }

        public ObjectId Id;
        public bool IsInteractable;

        public ShelfView ParentShelf { get; private set; }
        public int GridX;
        public int LayerIndex;

        public bool CanInteract => IsInteractable;
        public void InteractTapped() => OnTapped?.Invoke(this);
        public void InteractHeld() => OnHeld?.Invoke(this);
        
        private readonly Color32 FRONT_COLOR = new (255, 255, 255, 255);
        private readonly Color32 BACK_COLOR = new (125, 125, 125, 255);
        
        public void Init(ObjectId id, Sprite itemSprite, ShelfView parentShelf, int gridX, int layerIndex)
        {
            SetState(ObjectState.None);
            
            Id = id;
            ParentShelf = parentShelf;
            GridX = gridX;
            LayerIndex = layerIndex;
            
            Renderer.sprite = itemSprite;

            using var _ = ListPool<Vector2>.Get(out var points);
            itemSprite.GetPhysicsShape(0, points);
            Collider.SetPath(0, points);

            Renderer.sortingOrder = -layerIndex;
        }
        
        public void MoveToLocalPosition(Vector3 targetPos, bool animate)
        {
            // todo: should not be needing this
            if (transform.localPosition == targetPos)
                return;

            Tween.StopAll(transform);
            
            if (animate)
                Tween.LocalPosition(transform, targetPos, duration: 0.35f, ease: Ease.OutQuad);
            else
                transform.localPosition = targetPos;
        }

        public void OnRelease()
        {
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
            
            SetState(ObjectState.None);
        }
        
        /// <summary>
        /// Updates the item's visual and interactive state (as a state machine)
        /// </summary>
        public void SetState(ObjectState state)
        {
            if (State == state)
                return;
            
            // Transition out
            switch (State)
            {
                case ObjectState.Front:
                    IsInteractable = false;
                    Collider.enabled = false;
                    break;
                case ObjectState.Back:
                case ObjectState.Collected:
                case ObjectState.None:
                    break;
                case ObjectState.Hidden:
                    Renderer.enabled = true;
                    break;
            }

            // Transition in
            switch (state)
            {
                case ObjectState.Front:
                    IsInteractable = true;
                    Collider.enabled = true;
                    Renderer.color = FRONT_COLOR;
                    break;
                case ObjectState.Back:
                    Collider.enabled = false;
                    Renderer.color = BACK_COLOR;
                    break;
                case ObjectState.Hidden:
                    Renderer.enabled = false;
                    break;
                case ObjectState.Collected:
                    Renderer.color = FRONT_COLOR;
                    break;
                case ObjectState.None:
                    IsInteractable = false;
                    Collider.enabled = false;
                    break;
            }
            
            State = state;
        }
    }   
}
