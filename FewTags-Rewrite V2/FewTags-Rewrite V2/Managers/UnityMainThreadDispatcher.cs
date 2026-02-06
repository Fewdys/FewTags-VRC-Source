using FewTags.FewTags;
using UnityEngine;

namespace FewTags.FewTags_Rewrite_V2.Managers
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly Queue<Action> _queue = new Queue<Action>();
        int _count = 0;

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }

        public void Enqueue(Action action)
        {
            lock (_queue)
            {
                _queue.Enqueue(action);
            }
        }

        void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                {
                    try
                    {
                        _queue.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogErrorToConsole($"Error executing queued action: {ex}");
                    }
                }
            }
        }
    }
}
