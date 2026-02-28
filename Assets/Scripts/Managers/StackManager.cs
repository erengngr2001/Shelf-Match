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

        private const int MATCH_REQUIREMENT = 3;
        private const int LOGICAL_STACK_LIMIT = 5;
        
        private List<ObjectView> _logicStack = new List<ObjectView>(LOGICAL_STACK_LIMIT);
        private List<ObjectView> _visualStack = new List<ObjectView>(10);
        
        private Dictionary<ObjectView, StackItemState> _itemStates = new Dictionary<ObjectView, StackItemState>(10);

        private ObjectView[] _matchBuffer = new ObjectView[MATCH_REQUIREMENT];

        private bool _isVisualProcessing = false;

        public bool IsFull => _logicStack.Count >= LOGICAL_STACK_LIMIT;

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

            obj.transform.position = CameraUtilities.SwitchCameraSpace(
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

                if (matchCount >= MATCH_REQUIREMENT)
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
                        
                        if (removed == MATCH_REQUIREMENT) 
                            break;
                    }
                }
            }

            var levelScreen = LevelManager.Instance.LevelScreen;

            var visualIndex = Mathf.Min(_visualStack.Count - 1, levelScreen.StackSlots.Length - 1);
            var targetSlot = levelScreen.StackSlots[visualIndex];
            
            obj.Renderer.sortingOrder = 100 + _visualStack.Count - 1; 
            
            Tween.Position(obj.transform, targetSlot.position, 0.25f, Ease.OutQuad)
                // todo: allocates
                .OnComplete(this, (stack) => {
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
                        if (count < MATCH_REQUIREMENT) 
                            _matchBuffer[count] = otherObj;
                        
                        count++;
                    }
                }

                if (count >= MATCH_REQUIREMENT)
                {
                    matchedId = _visualStack[i].Id.Value;
                    break;
                }
            }

            if (matchedId != string.Empty)
            {
                _isVisualProcessing = true;

                var matchSeq = Sequence.Create();
                for (var i = 0; i < MATCH_REQUIREMENT; i++)
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
                    for (var i = 0; i < MATCH_REQUIREMENT; i++)
                    {
                        var item = buffer[i];
                        _visualStack.Remove(item);      
                        _itemStates.Remove(item);
                        
                        item.OnRelease();
                        GamePools.Instance.ObjectViewPool.Release(item);
                        buffer[i] = null; 
                    }
                    
                    LevelManager.Instance.OnItemsMatched(MATCH_REQUIREMENT);
                    _isVisualProcessing = false;
                    
                    SlideRemainingItems(); 
                    ProcessVisualStack(); 
                });
            }
            else if (_logicStack.Count >= LOGICAL_STACK_LIMIT && 
                     AreAllVisualItemsResting())
            {
                LevelManager.Instance.TriggerFailState();
            }
        }

        private void SlideRemainingItems()
        {
            var levelScreen = LevelManager.Instance.LevelScreen;
            
            for (var i = 0; i < _visualStack.Count; i++)
            {
                var item = _visualStack[i];
                var targetIndex = Mathf.Min(i, levelScreen.StackSlots.Length - 1);
                
                if (_itemStates[item] == StackItemState.Flying)
                {
                    item.StopAllMovement();
                    item.Renderer.sortingOrder = 100 + i;
                    
                    Tween.Position(item.transform, levelScreen.StackSlots[targetIndex].position, 0.25f, Ease.OutQuad)
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

            var levelScreen = LevelManager.Instance.LevelScreen;
            
            var targetIndex = Mathf.Min(_visualStack.IndexOf(item), levelScreen.StackSlots.Length - 1);
            var currentVisualSlot = FindClosestSlotIndex(item.transform.position.x);

            if (currentVisualSlot > targetIndex && 
                _itemStates[item] != StackItemState.Jumping)
            {
                _itemStates[item] = StackItemState.Jumping;
                item.Renderer.sortingOrder = 100 + targetIndex; 

                var nextIdx = currentVisualSlot - 1;
                var nextPos = levelScreen.StackSlots[nextIdx].position;
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
            
            var levelScreen = LevelManager.Instance.LevelScreen;
            
            for (var i = 0; i < levelScreen.StackSlots.Length; i++) 
            {
                var distance = Mathf.Abs(levelScreen.StackSlots[i].position.x - xPos);
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