using UnityEngine;

namespace Common.Network
{
    public abstract class AServer : MonoBehaviour
    {
        private Network _network;

        public void Initialize(Network network)
        {
            _network = network;

            Initialize();
        }

        public abstract void Initialize();

        protected T GetModule<T>() where T : ANetworkModuleServer => _network.GetModuleServer<T>();
    }
}