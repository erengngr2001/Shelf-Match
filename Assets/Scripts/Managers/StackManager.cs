using System.Collections.Generic;
using Game;
using Level.Objects;
using PrimeTween;
using UnityEngine;

namespace Managers
{
    public class StackManager : MonoBehaviour, IManualUpdate
    {
        private enum StackItemState
        {
            Flying,
            Resting,
            Jumping,
            Matching
        }

        public Transform[] StackSlots;
        public Transform Stack;

        private List<ObjectView> _logicStack = new List<ObjectView>(5);
        private List<ObjectView> _visualStack = new List<ObjectView>(10);
        
        private Dictionary<ObjectView, StackItemState> _itemStates = new Dictionary<ObjectView, StackItemState>(10);
        
        private ObjectView[] _matchBuffer = new ObjectView[3];

        private bool _isFailing = false;
        private bool _isVisualProcessing = false;

        public bool IsFull => _logicStack.Count >= 5 || _isFailing;

         public void ManualUpdate()
         {
             // Frame-by-frame stack logic
         }

        public void AddItem(ObjectView obj)
        {
            if (IsFull) return;

            _logicStack.Add(obj);
            _visualStack.Add(obj);
            
            obj.SetState(ObjectState.Collected);
            _itemStates[obj] = StackItemState.Flying;

            obj.transform.position = GameManager.Instance.SwitchCameraSpace(
                obj.transform.position, 
                GameManager.Instance.EnvironmentCamera, 
                GameManager.Instance.StackCamera
            );
            obj.gameObject.layer = LayerMask.NameToLayer("Stack");

            var hasLogicalMatch = false;
            var matchedLogicId = string.Empty;
            
            for (var i = 0; i < _logicStack.Count; i++)
            {
                var matchCount = 1;
                for (var j = i + 1; j < _logicStack.Count; j++)
                {
                    if (_logicStack[i].Id.Value == _logicStack[j].Id.Value) 
                        matchCount++;
                }

                if (matchCount >= 3)
                {
                    hasLogicalMatch = true;
                    matchedLogicId = _logicStack[i].Id.Value;
                    break;
                }
            }

            if (hasLogicalMatch)
            {
                var removed = 0;
                for (var k = _logicStack.Count - 1; k >= 0; k--)
                {
                    if (_logicStack[k].Id.Value == matchedLogicId)
                    {
                        _logicStack.RemoveAt(k);
                        removed++;
                        
                        if (removed == 3) 
                            break;
                    }
                }
            }
            else if (_logicStack.Count >= 5)
            {
                _isFailing = true; 
            }

            var visualIndex = Mathf.Min(_visualStack.Count - 1, StackSlots.Length - 1);
            var targetSlot = StackSlots[visualIndex];
            
            obj.Renderer.sortingOrder = 100 + _visualStack.Count - 1; 
            
            var moveSeq = Sequence.Create();
            obj.AssignSequence(moveSeq);
            
            moveSeq.Group(Tween.Position(obj.transform, targetSlot.position, 0.25f, Ease.OutQuad))
                   .Group(Tween.Scale(obj.transform, obj.DefaultScale, 0.25f, Ease.OutQuad));
            
            // todo: allocates
            moveSeq.OnComplete(this, (stack) => {
                stack._itemStates[obj] = StackItemState.Resting;
                stack.ProcessVisualStack();
            });
        }
        
        public void Clear()
        {
            foreach (var item in _visualStack)
            {
                if (item == null)
                    continue;
                
                item.StopAllMovement();
                item.OnRelease();
                GamePools.Instance.ObjectViewPool.Release(item);
            }

            _logicStack.Clear();
            _visualStack.Clear();
            _itemStates.Clear();

            for (var i = 0; i < _matchBuffer.Length; i++) 
                _matchBuffer[i] = null;

            _isFailing = false;
            _isVisualProcessing = false;
        }
        
        public ObjectView PeekLastItem()
        {
            if (_logicStack.Count == 0) 
                return null;
            
            return _logicStack[_logicStack.Count - 1];
        }

        public ObjectView PopLastItemForUndo()
        {
            if (_logicStack.Count == 0) 
                return null;

            var item = _logicStack[_logicStack.Count - 1];
            _logicStack.RemoveAt(_logicStack.Count - 1);
    
            _visualStack.Remove(item);
            _itemStates.Remove(item);
    
            // todo: What happens if I undo after losing? Temporarily reversed the failing state
            _isFailing = false; 

            item.StopAllMovement();

            return item;
        }

        private void ProcessVisualStack()
        {
            if (_isVisualProcessing) 
                return;

            var matchedId = string.Empty;

            for (var i = 0; i < _visualStack.Count; i++)
            {
                var mainObj = _visualStack[i];
                
                if (_itemStates[mainObj] != StackItemState.Resting) 
                    continue;

                var count = 0;
                for (var j = i; j < _visualStack.Count; j++)
                {
                    var otherObj = _visualStack[j];
                    
                    if (mainObj.Id.Value == otherObj.Id.Value && 
                        _itemStates[otherObj] == StackItemState.Resting)
                    {
                        if (count < 3) 
                            _matchBuffer[count] = otherObj;
                        
                        count++;
                    }
                }

                if (count >= 3)
                {
                    matchedId = _visualStack[i].Id.Value;
                    break;
                }
            }

            if (matchedId != string.Empty)
            {
                _isVisualProcessing = true;

                var matchSeq = Sequence.Create();
                for (var i = 0; i < 3; i++)
                {
                    var item = _matchBuffer[i];
                    item.AssignSequence(matchSeq);
                    _itemStates[item] = StackItemState.Matching;
                    item.Renderer.sortingOrder = 200; 
    
                    var targetWorldY = item.transform.position.y + 1.5f;
                    var targetScale = item.transform.localScale * 1.3f;

                    matchSeq.Group(Tween.PositionY(item.transform, targetWorldY, 0.3f, Ease.OutBack))
                            .Group(Tween.Scale(item.transform, targetScale, 0.3f, Ease.OutBack));
                }

                // todo: allocates
                matchSeq.OnComplete(_matchBuffer, (buffer) => 
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var item = buffer[i];
                        _visualStack.Remove(item);      
                        _itemStates.Remove(item);
                        
                        item.OnRelease();
                        GamePools.Instance.ObjectViewPool.Release(item);
                        buffer[i] = null; 
                    }
                    
                    LevelManager.Instance.OnItemsMatched(3);
                    _isVisualProcessing = false;
                    
                    SlideRemainingItems(); 
                    ProcessVisualStack(); 
                });
            }
            else if (_isFailing && 
                     AreAllVisualItemsResting())
            {
                LevelManager.Instance.TriggerFailState();
            }
        }

        private void SlideRemainingItems()
        {
            for (var i = 0; i < _visualStack.Count; i++)
            {
                var item = _visualStack[i];
                var targetIndex = Mathf.Min(i, StackSlots.Length - 1);
                
                if (_itemStates[item] == StackItemState.Flying)
                {
                    item.StopAllMovement();
                    item.Renderer.sortingOrder = 100 + i;
                    
                    Tween.Position(item.transform, StackSlots[targetIndex].position, 0.25f, Ease.OutQuad)
                        // todo: allocates
                         .OnComplete(this, (stack) => {
                             stack._itemStates[item] = StackItemState.Resting;
                             stack.ProcessVisualStack();
                         });
                }
                else
                {
                    TryJumpLeft(item);
                }
            }
        }

        private void TryJumpLeft(ObjectView item)
        {
            if (_itemStates[item] == StackItemState.Matching) 
                return;

            var targetIndex = Mathf.Min(_visualStack.IndexOf(item), StackSlots.Length - 1);
            var currentVisualSlot = FindClosestSlotIndex(item.transform.position.x);

            if (currentVisualSlot > targetIndex && 
                _itemStates[item] != StackItemState.Jumping)
            {
                _itemStates[item] = StackItemState.Jumping;
                item.Renderer.sortingOrder = 100 + targetIndex; 

                var nextIdx = currentVisualSlot - 1;
                var nextPos = StackSlots[nextIdx].position;
                var jumpDuration = 0.18f;

                var stepSeq = Sequence.Create();
                item.AssignSequence(stepSeq);
                stepSeq.Group(Tween.PositionX(item.transform, nextPos.x, jumpDuration, Ease.Linear));

                var ySeq = Sequence.Create();
                ySeq.Chain(Tween.PositionY(item.transform, nextPos.y + 0.6f, jumpDuration / 2f, Ease.OutQuad));
                ySeq.Chain(Tween.PositionY(item.transform, nextPos.y, jumpDuration / 2f, Ease.InQuad));

                stepSeq.Group(ySeq);
                
                stepSeq.OnComplete(this, (stack) => {
                    stack._itemStates[item] = StackItemState.Resting;
                    stack.TryJumpLeft(item); 
                });
            }
            else if (currentVisualSlot <= targetIndex && 
                     _itemStates[item] == StackItemState.Resting)
            {
                ProcessVisualStack();
            }
        }

        private bool AreAllVisualItemsResting()
        {
            for (var i = 0; i < _visualStack.Count; i++)
            {
                if (_itemStates[_visualStack[i]] != StackItemState.Resting) 
                    return false;
            }
            return true;
        }

        private int FindClosestSlotIndex(float xPos)
        {
            var closestIdx = 0;
            var minDistance = float.MaxValue;
            
            for (var i = 0; i < StackSlots.Length; i++) 
            {
                var distance = Mathf.Abs(StackSlots[i].position.x - xPos);
                if (distance < minDistance) 
                {
                    minDistance = distance;
                    closestIdx = i;
                }
            }
            return closestIdx;
        }
    }
}