//异步处理信息时，同步更新客户端的ui逻辑
using System;
using System.Collections.Generic;
using UnityEngine;
namespace CardGameApp
{
    //使用分配器实现网络和游戏的交互
    public class MainDispatcher : MonoBehaviour
    {
        private static bool _initialized = false;
        private static volatile bool _actionExecuting = false;
        private static readonly List<Action> _excuteOnMain = new List<Action>();
        private static readonly List<Action> _copyExcuteOnMain = new List<Action>(); 
        private static MainDispatcher _instance = null;  
        public static MainDispatcher Instance { get { return _instance; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            if (_initialized) { return; }
            _initialized = true;
            _instance = FindFirstObjectByType<MainDispatcher>();
            if (_instance == null)
            {
                _instance = new GameObject("RealtimeNetworkingThreadDispatcher").AddComponent<MainDispatcher>();
            }
            DontDestroyOnLoad(_instance.gameObject);
        }

        private void Awake()
        {
            Initialize();
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if(_instance == this) 
                _instance = null;
        }
        //让其在主线程中执行
        private void Update()
        {
            if (!_actionExecuting) return;
            lock (_excuteOnMain)
            {
                _copyExcuteOnMain.AddRange(_excuteOnMain);
                _excuteOnMain.Clear();
                _actionExecuting = false;
            }

            // 执行时放在 lock 外面
            for (int i = 0; i < _copyExcuteOnMain.Count; i++)
            {
                _copyExcuteOnMain[i]?.Invoke();
            }
            _copyExcuteOnMain.Clear(); 
        }
        
        public void ExecuteOnMainThread(Action action)
        {
            if (_instance == null)
            {
                Debug.Log("Threading not initialized.");
                return;
            }
            if (action == null)
            {
                Debug.Log("No action to execute on main thread.");
                return;
            }
            lock (_excuteOnMain)
            {
                _excuteOnMain.Add(action);
                _actionExecuting = true;
            }
        }
    }
    
}