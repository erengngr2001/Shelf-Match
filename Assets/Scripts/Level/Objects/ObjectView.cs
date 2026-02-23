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
        public BoxCollider2D Collider;
        
        public ObjectState State { get; private set; } 
    }   
}
