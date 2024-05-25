using System;

namespace Common.Network
{
    public interface INetworkServerRequire<out T> where T : ANetworkModuleServer
    {
        void AddDependency(ANetworkModuleServer obj);

        Type Type() => typeof(T);
    }
}
