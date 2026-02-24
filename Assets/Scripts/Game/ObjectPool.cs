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
        private Transform _parent;

        public T Prefab;
        public int AutoFillCount;

        public ObjectPool(int capacity)
        {
            _pool = new List<T>(capacity);
        }

        public void Init()
        {
            Assert.IsNull(_parent, "Pool is already initialized");
            
            if (_pool == null)
            {
                var capacity = Mathf.Max(16, AutoFillCount);
                _pool = new List<T>(capacity);
            } 
            else
            {
                // Fixes domain reload issues
                _pool.Clear();
            }

            // Create a clean container for the pooled objects in the current scene
            var go = new GameObject($"{Prefab.name}_Pool");
            _parent = go.transform;

            if (AutoFillCount > 0)
            {
                Fill(AutoFillCount);
            }
        }

        public void Dispose()
        {
            if (_parent != null)
            {
                Object.Destroy(_parent.gameObject);
            }

            _parent = null;
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
            
                obj.transform.SetParent(null);
                obj.gameObject.SetActive(true);
            
                return obj;
            }
        
            var objNew = Object.Instantiate(Prefab);
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
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent);
            _pool.Add(obj);
        }

        public void Fill(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(Prefab, _parent);
                obj.name = Prefab.name;
            
                Release(obj);
            }
        }
    }
}