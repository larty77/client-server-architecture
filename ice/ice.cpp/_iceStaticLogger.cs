using System.Runtime.InteropServices;

namespace ICENet.dll
{
    public class _iceStaticLogger
    {
        #region external

        public delegate void Log(string message);

        private const string _dllPath = "ice_net";

        [DllImport(_dllPath)]
        private static extern void logger_set_info(Log action);

        [DllImport(_dllPath)]
        private static extern void logger_set_error(Log action);

        #endregion

        private static _iceStaticLogger _instance;
        
        public static _iceStaticLogger Instance
        {
            get
            {
                _instance ??= new _iceStaticLogger();

                return _instance;
            }
        }

        public Log Info;

        public Log Error;

        private _iceStaticLogger()
        {
            logger_set_info(LogInfo);
            logger_set_error(LogError);
        }

        private static void LogInfo(string msg) => _instance.Info?.Invoke(msg);

        private static void LogError(string msg) => _instance.Error?.Invoke(msg);

        ~_iceStaticLogger()
        {
            logger_set_info(null);
            logger_set_error(null);
        }
    }
}