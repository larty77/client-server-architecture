using ICENet;

using UnityEngine;

namespace Common.Network
{
    public abstract class ANetworkModuleClient : MonoBehaviour
    {
        protected IceClient _client;

        private Network _network;

        #region External

        public void Register(Network network)
        {
            _network = network;
            (this as INetworkModuleClientType<ANetworkModuleClient>).TRegister(this);
        } //From Network

        public void OnDestroy()
        {
            if (_network == null) return;
            (this as INetworkModuleClientType<ANetworkModuleClient>).TDestroy(this); 
        }

        #endregion

        #region Internal

        public void Register<T>() where T : ANetworkModuleClient => _network.AddModuleClient(GetComponent<T>()); //From INetworkModuleClientType     

        public void Destroy<T>() where T : ANetworkModuleClient => _network.RemoveModuleClient<T>(); //From INetworkModuleClientType

        #endregion

        #region Callback

        protected virtual void OnSetup() { }

        public void Setup(IceClient client)
        {
            _client = client;
            OnSetup();
        }

        #endregion
    }

    public interface INetworkModuleClientType<out T> where T : ANetworkModuleClient
    {
        void TRegister(ANetworkModuleClient client) => client.Register<T>();

        void TDestroy(ANetworkModuleClient client) => client.Destroy<T>();
    }
}
