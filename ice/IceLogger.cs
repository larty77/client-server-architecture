using UnityEngine;
using UnityEngine.Events;

using ICENet.dll;

namespace ICENet
{
    public class IceLogger : MonoBehaviour
    {
        [SerializeField] private UnityEvent<string> _logInfo;

        [SerializeField] private UnityEvent<string> _logError;

        public static IceLogger Instance { get; private set; }

        void Awake()
        {
            Instance = this;

            _iceStaticLogger.Instance.Info = LogInfo;
            _iceStaticLogger.Instance.Error = LogError;
        }

        public void LogInfo(string msg)
        {
            Debug.Log(msg);

            _logInfo.Invoke(msg);
        }

        public void LogError(string msg)
        {
            Debug.LogError(msg);

            _logError.Invoke(msg);
        }
    }
}