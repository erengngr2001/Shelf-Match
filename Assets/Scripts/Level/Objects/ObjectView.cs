using System.Collections.Generic;
using UnityEngine;

namespace Level.Objects
{
    public enum ObjectState
    {
        Front,
        Back,
        MovingToStack
    }
    
    public class ObjectView : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        public PolygonCollider2D Collider;
        
        public ObjectState State { get; private set; }

        public ObjectId Id;
        public Vector3 OriginalShelfPosition;
        public bool IsInteractable;

        public int ShelfIndex;
        public int LayerIndex;

        private bool _firstInitialization = true;
        
        private readonly Color32 FRONT_COLOR = new (255, 255, 255, 255);
        private readonly Color32 BACK_COLOR = new (125, 125, 125, 255);
        
        public void Init(ObjectId id, Sprite itemSprite, Vector3 spawnPosition, int shelfIndex, int layerIndex)
        {
            Id = id;
            OriginalShelfPosition = spawnPosition;
            transform.position = spawnPosition;
            ShelfIndex = shelfIndex;
            LayerIndex = layerIndex;
            // todo: SetState by checking layerIndex (whether it is front or back)
            
            Renderer.sprite = itemSprite;

            // todo: pool list
            var points = new List<Vector2>();
            itemSprite.GetPhysicsShape(0, points);
            Collider.SetPath(0, points);

            if (!_firstInitialization)
                transform.localScale = Vector3.one; 
            
            _firstInitialization = false;
        }

        public void OnRelease()
        {
            
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
                case ObjectState.Back:
                case ObjectState.MovingToStack:
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
                    IsInteractable = false;
                    Collider.enabled = false;
                    Renderer.color = BACK_COLOR;
                    break;
                case ObjectState.MovingToStack:
                    IsInteractable = false;
                    Collider.enabled = false;
                    Renderer.color = FRONT_COLOR;
                    break;
            }
            
            State = state;
        }
    }   
}
