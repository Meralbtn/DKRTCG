using UnityEngine;

namespace SingletonMonoModule
{
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        private void OnDestroy()
        {
            // 销毁时清空静态引用
            if (_instance == this)
                _instance = null;
        }

        // 主动销毁的入口
        public static void DestroySelf()
        {
            if (_instance != null)
                Destroy(_instance.gameObject);
        }

    }
}