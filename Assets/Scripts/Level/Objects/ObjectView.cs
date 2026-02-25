using Game;
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
        MovingToStack
    }
    
    public class ObjectView : MonoBehaviour, IPoolable
    {
        public SpriteRenderer Renderer;
        public PolygonCollider2D Collider;
        
        public ObjectState State { get; private set; }

        public ObjectId Id;
        public Vector3 OriginalShelfPosition;
        public bool IsInteractable;

        public int ShelfIndex;
        public int GridX;
        public int LayerIndex;

        private bool _firstInitialization = true;
        
        private readonly Color32 FRONT_COLOR = new (255, 255, 255, 255);
        private readonly Color32 BACK_COLOR = new (125, 125, 125, 255);
        
        public void Init(ObjectId id, Sprite itemSprite, int shelfIndex, int gridX, int layerIndex)
        {
            // todo: never directly modify the State
            State = ObjectState.None;
            
            Id = id;
            ShelfIndex = shelfIndex;
            GridX = gridX;
            LayerIndex = layerIndex;
            
            Renderer.sprite = itemSprite;

            using var _ = ListPool<Vector2>.Get(out var points);
            itemSprite.GetPhysicsShape(0, points);
            Collider.SetPath(0, points);

            if (!_firstInitialization)
                transform.localScale = Vector3.one; 
            
            _firstInitialization = false;

            // switch (layerIndex)
            // {
            //     case 0:
            //         SetState(ObjectState.Front);
            //         break;
            //     case 1:
            //         SetState(ObjectState.Back);
            //         break;
            //     default:
            //         SetState(ObjectState.Hidden);
            //         break;
            // }
            Renderer.sortingOrder = -layerIndex;
        }
        
        public void MoveToLocalPosition(Vector3 targetPos, bool animate)
        {
            Tween.StopAll(transform); // Prevent tweens from fighting
            
            if (animate)
            {
                // Smoothly slide the object to its new active layer position
                Tween.LocalPosition(transform, targetPos, duration: 0.35f, ease: Ease.OutQuad);
            }
            else
            {
                // Instantly snap to position (used during initial level generation)
                transform.localPosition = targetPos;
            }
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
                case ObjectState.MovingToStack:
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
                    Renderer.color = BACK_COLOR;
                    break;
                case ObjectState.Hidden:
                    Renderer.enabled = false;
                    break;
                case ObjectState.MovingToStack:
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
