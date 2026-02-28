using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Game
{
    public readonly struct ObjectPoolHandle<T> : IDisposable where T : Component
    {
        public readonly T Obj;
        public readonly ObjectPool<T> Pool;

        public ObjectPoolHandle(T obj, ObjectPool<T> pool)
        {
            Obj = obj;
            Pool = pool;
        }

        public void Dispose()
        {
            Pool.Release(Obj);
        }
    }

    [Serializable]
    public class ObjectPool<T> : IDisposable where T : Component
    {
        private List<T> _pool;
        private Transform _inactiveContainer;
        private Transform _activeContainer; 

        public T Prefab;
        public int AutoFillCount;

        public ObjectPool(int capacity)
        {
            _pool = new List<T>(capacity);
        }

        public void Init()
        {
            Assert.IsNull(_inactiveContainer, "Pool is already initialized");
            
            if (_pool == null)
            {
                var capacity = Mathf.Max(16, AutoFillCount);
                _pool = new List<T>(capacity);
            } 
            else
            {
                _pool.Clear();
            }

            // Create clean containers. We explicitly set scales to Vector3.one to guarantee safety.
            var inactiveGo = new GameObject($"{Prefab.name}_InactivePool");
            _inactiveContainer = inactiveGo.transform;
            _inactiveContainer.localScale = Vector3.one;
            inactiveGo.SetActive(false); 

            var activeGo = new GameObject($"{Prefab.name}_ActiveItems");
            _activeContainer = activeGo.transform;
            _activeContainer.localScale = Vector3.one;

            if (AutoFillCount > 0)
            {
                Fill(AutoFillCount);
            }
        }

        public void Dispose()
        {
            if (_inactiveContainer != null) 
                Object.Destroy(_inactiveContainer.gameObject);
            
            if (_activeContainer != null) 
                Object.Destroy(_activeContainer.gameObject);

            _inactiveContainer = null;
            _activeContainer = null;
            _pool?.Clear();
            _pool = null;
        } 
        
        public T Get()
        {
            if (_pool.Count > 0)
            {
                var index = _pool.Count - 1;
                var obj = _pool[index];
            
                _pool.RemoveAt(index);
            
                // Parent to the safe, unscaled active container
                obj.transform.SetParent(_activeContainer, false);
                obj.gameObject.SetActive(true);
            
                return obj;
            }
        
            var objNew = Object.Instantiate(Prefab, _activeContainer);
            objNew.name = Prefab.name;
            return objNew;
        }

        public ObjectPoolHandle<T> Get(out T obj)
        {
            obj = Get();
            return new ObjectPoolHandle<T>(obj, this);
        }

        public void Release(T obj)
        {
#if UNITY_EDITOR
            if (_pool.Contains(obj))
                throw new Exception($"Double release detected for {obj.name}!");
#endif
            
            if (obj is IPoolable poolable)
                poolable.OnRelease();
            
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_inactiveContainer, false);
            _pool.Add(obj);
        }

        public void Fill(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(Prefab, _inactiveContainer);
                obj.name = Prefab.name;
            
                Release(obj);
            }
        }
    }
}