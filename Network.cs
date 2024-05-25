using ICENet;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Common.Network
{
    public class Network : MonoBehaviour
    {
        [SerializeField] private IceServer _iceServer;

        [SerializeField] private IceClient _iceClient;

        [SerializeField] private UnityEvent _close;

        #region Initialize

        private enum SocketType
        {
            None,
            Client,
            Server,
        }

        private SocketType _currentSocketType = SocketType.None;

        private void Awake() => SceneManager.activeSceneChanged += Initialize;

        private void Initialize(Scene s, Scene c)
        {
            if (_currentSocketType == SocketType.None) return;

            _networkServerModules = new();
            _networkClientModules = new();

            Action<Network> initModule = _currentSocketType == SocketType.Server ?
                (n) => FindObjectsOfType<ANetworkModuleServer>().ToList().ForEach(x => x.Register(n)) :
                (n) => FindObjectsOfType<ANetworkModuleClient>().ToList().ForEach(x => x.Register(n));

            Action initRequirements = _currentSocketType == SocketType.Server ?
                () => FindObjectsOfType<ANetworkModuleServer>().OfType<INetworkServerRequire<ANetworkModuleServer>>().ToList().ForEach(x => x.AddDependency(_networkServerModules.FirstOrDefault(y => y.GetType() == x.Type()))) :
                () => FindObjectsOfType<ANetworkModuleClient>().OfType<INetworkClientRequire<ANetworkModuleClient>>().ToList().ForEach(x => x.AddDependency(_networkClientModules.FirstOrDefault(y => y.GetType() == x.Type())));

            Action <Network> initSocket = _currentSocketType == SocketType.Server ?
                (n) => FindAnyObjectByType<AServer>()?.Initialize(n) :
                (n) => FindAnyObjectByType<AClient>()?.Initialize(n);
                        
            initModule(this);
            initRequirements();
            initSocket(this); 
        }

        public void SwitchToServer() { _currentSocketType = SocketType.Server; Initialize(default, default); }

        public void SwitchToClient() { _currentSocketType = SocketType.Client; Initialize(default, default); }

        public void Close() => _close.Invoke();

        private void OnDestroy() => SceneManager.activeSceneChanged -= Initialize;

        #endregion

        #region Modules

        private List<ANetworkModuleServer> _networkServerModules = new();

        public void AddModuleServer<T>(T module) where T : ANetworkModuleServer
        {
            if (_networkServerModules.FirstOrDefault(x => x.GetType() == typeof(T)) != null) return;

            _networkServerModules.Add(module);
            module.Setup(_iceServer);
        }

        public T GetModuleServer<T>() where T : ANetworkModuleServer
        {
            var module = _networkServerModules.FirstOrDefault(x => x is T);
            return (T)module;
        }

        public void RemoveModuleServer<T>() where T : ANetworkModuleServer
        {
            var module = GetModuleServer<T>();

            if (module == null) return;

            if (!_networkServerModules.Contains(module)) return;

            _networkServerModules.Remove(module);
        }

        private List<ANetworkModuleClient> _networkClientModules = new();

        public void AddModuleClient<T>(T module) where T : ANetworkModuleClient
        {
            if (_networkClientModules.FirstOrDefault(x => x is T) != null) return;

            _networkClientModules.Add(module);
            module.Setup(_iceClient);
        }

        public T GetModuleClient<T>() where T : ANetworkModuleClient
        {
            var module = _networkClientModules.FirstOrDefault(x => x is T);

            if (module != null && module is T typedModule) return typedModule;

            return null;
        }

        public void RemoveModuleClient<T>() where T : ANetworkModuleClient
        {
            var module = GetModuleClient<T>();

            if (module == null) return;

            if (!_networkClientModules.Contains(module)) return;

            _networkClientModules.Remove(module);
        }

        #endregion
    }
}