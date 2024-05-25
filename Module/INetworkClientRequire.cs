using System;

namespace Common.Network
{
    public interface INetworkClientRequire<out T> where T : ANetworkModuleClient
    {
        void AddDependency(ANetworkModuleClient obj);

        Type Type() => typeof(T);
    }
}
