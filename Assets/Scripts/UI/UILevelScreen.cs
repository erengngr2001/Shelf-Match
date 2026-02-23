using UnityEngine;

namespace UI
{
    public class UILevelScreen : MonoBehaviour
    {
        public SpriteRenderer TopPanel;    
        public SpriteRenderer StackPanel; 

        public Transform[] StackSlotTransforms;

        private void OnUndoClicked()
        {
            // todo: Notify UndoManager
            Debug.Log("Undo Clicked");
        }

        private void OnRestartClicked()
        {
            // todo: Notify GameManager
            Debug.Log("Restart Clicked");
        }
        
        /// <summary>
        /// Called by the StackManager to find where an item should physically move
        /// </summary>
        public Transform GetStackSlotTransform(int index)
        {
            if (index < 0 || index >= StackSlotTransforms.Length)
            {
                Debug.LogWarning($"Stack slot index {index} is out of bounds!");
                return null;
            }
            
            return StackSlotTransforms[index];
        }
    }
}