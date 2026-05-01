using System.Collections.Generic;
using UnityEngine;

//对象池模板
public class GameObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _pool = new Stack<T>();
    public GameObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewInstance();
        }
    }

    private T CreateNewInstance()
    {
        T instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);
        _pool.Push(instance);
        return instance;
    }

    // 从池子中获取对象
    public T Get()
    {
        T instance = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(true);
        return instance;
    }

    // 将对象还回池子
    public void Return(T instance)
    {
        instance.gameObject.SetActive(false);
        _pool.Push(instance);
    }

    // 回收所有子物体（清空列表时用）
    public void ReturnAll(List<T> activeList)
    {
        foreach (var item in activeList)
        {
            Return(item);
        }
        activeList.Clear();
    }
}