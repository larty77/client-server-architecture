using UnityEngine;

namespace Common.Network
{
    public abstract class AClient : MonoBehaviour
    {
        private Network _network;

        public void Initialize(Network network)
        {
            _network = network;

            Initialize();
        }

        public abstract void Initialize();

        protected T GetModule<T>() where T : ANetworkModuleClient => _network.GetModuleClient<T>();
    }
}