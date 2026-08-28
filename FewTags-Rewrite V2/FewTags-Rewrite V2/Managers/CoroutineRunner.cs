using BepInEx.Unity.IL2CPP.Utils;
using FewTags.FewTags;
using System.Collections;
using UnityEngine;

namespace FewTags.FewTags_Rewrite_V2.Managers
{
    public static class CoroutineHelper
    {
        private class Runner : MonoBehaviour { }

        private static Runner _runner;
        private static GameObject _runnerGameObject;
        private static readonly Dictionary<IEnumerator, Coroutine> _activeCoroutines = new();

        private static void EnsureRunner()
        {
            if (_runner != null) return;

            if (_runnerGameObject != null)
            {
                _runner = _runnerGameObject.GetComponent<Runner>();
                if (_runner != null) return;
            }
            else
            {
                _runnerGameObject = new GameObject("CoroutineHelperRunner");
                UnityEngine.Object.DontDestroyOnLoad(_runnerGameObject);
                _runner = _runnerGameObject.AddComponent<Runner>();
            }
        }

        public static void Run(IEnumerator coroutine)
        {
            EnsureRunner();
            var c = _runner.StartCoroutine(coroutine);
            _activeCoroutines[coroutine] = c;
        }

        public static IEnumerator _Run(IEnumerator coroutine)
        {
            EnsureRunner();
            var c = _runner.StartCoroutine(coroutine);
            _activeCoroutines[coroutine] = c;
            yield return c;
        }

        public static void RunSafe(IEnumerator coroutine)
        {
            Run(SafeCoroutine(coroutine));
        }

        public static IEnumerator _RunSafe(IEnumerator coroutine)
        {
            yield return _Run(SafeCoroutine(coroutine));
        }

        private static IEnumerator SafeCoroutine(IEnumerator coroutine)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!coroutine.MoveNext()) break;
                    current = coroutine.Current;
                }
                catch (Exception e)
                {
                    LogManager.LogErrorToConsole(e.ToString());
                    break;
                }
                yield return current;
            }
        }

        public static void Stop(IEnumerator coroutine)
        {
            if (_activeCoroutines.TryGetValue(coroutine, out var c))
            {
                _runner.StopCoroutine(c);
                _activeCoroutines.Remove(coroutine);
            }
        }
    }
}
