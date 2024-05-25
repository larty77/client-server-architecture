using ICENet;

using UnityEngine;

namespace Common.Network
{
    public abstract class ANetworkModuleServer : MonoBehaviour
    {
        protected IceServer _server;

        private Network _network;

        #region External

        public void Register(Network network)
        {
            _network = network;
            (this as INetworkModuleServerType<ANetworkModuleServer>).TRegister(this);
        } //From Network

        public void OnDestroy()
        {
            if (_network == null) return;
            (this as INetworkModuleServerType<ANetworkModuleServer>).TDestroy(this);
        }

        #endregion

        #region Internal

        public void Register<T>() where T : ANetworkModuleServer => _network.AddModuleServer(GetComponent<T>()); //From INetworkModuleServerType     

        public void Destroy<T>() where T : ANetworkModuleServer => _network.RemoveModuleServer<T>(); //From INetworkModuleServerType

        #endregion

        #region Callback

        protected virtual void OnSetup() { }

        public void Setup(IceServer server)
        {
            _server = server;
            OnSetup();
        }

        #endregion
    }

    public interface INetworkModuleServerType<out T> where T : ANetworkModuleServer
    {
        void TRegister(ANetworkModuleServer server) => server.Register<T>();

        void TDestroy(ANetworkModuleServer server) => server.Destroy<T>();
    }
}