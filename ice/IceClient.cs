using UnityEngine;
using UnityEngine.Events;

using System.Collections.Concurrent;
using System.Reflection;
using System;

using ICENet.dll;
using ICENet.Traffic;

namespace ICENet
{
    public class IceClient : MonoBehaviour, IClientHandler
    {
        private _iceClientSocket _socket;

        public class TrafficHelper
        {
            private readonly PacketFactory _packetFactory;

            private readonly ConcurrentDictionary<Type, Action<Packet>> _handlers = new();

            private readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new();

            public TrafficHelper()
            {
                _packetFactory = new PacketFactory();
            }

            public void AddOrUpdateHandler<T>(Action<T> handleAction) where T : Packet, new()
            {
                if (_packetFactory.AddOrUpdate<T>() is true)
                    _handlers[typeof(T)] = (packet) => handleAction((T)packet);
            }

            internal Packet GetPacketInstance(ref Traffic.Buffer data) => _packetFactory.GetInstance(ref data);

            internal void InvokeHandler(Packet packet)
            {
                try
                {
                    Type packetType = packet.GetType();

                    if (_methodCache.TryGetValue(packetType, out MethodInfo method) is false)
                        _methodCache[packetType] = GetType().GetMethod(nameof(InvokeHandlerGeneric), BindingFlags.Instance |
                            BindingFlags.NonPublic).MakeGenericMethod(packetType);

                    method = _methodCache[packetType];

                    method.Invoke(this, new object[] { packet });
                }
                catch {  }
            }

            private void InvokeHandlerGeneric<T>(T packet) where T : Packet, new()
            {
                try { _handlers[packet!.GetType()].Invoke(packet); } catch { }
            }
        }

        public TrafficHelper Traffic => _traffic;

        private TrafficHelper _traffic = new TrafficHelper();

        public void Connect(string address, ushort port)
        {
            Release();

            _socket = new _iceClientSocket(GetComponent<IceClient>());
            _socket.Bind(0);
            _socket.Connect(address, port);
        }

        private void Update()
        {
            float interval = 1.0f / 1000.0f; 
            float last = 0.0f;

            last += Time.fixedDeltaTime;

            while (last >= interval)
            {
                _socket?.Update();

                last -= interval;
            }
        }

        public void Send(Packet packet)
        {
            Traffic.Buffer data = new(0);

            packet.Serialize(ref data);

            _socket.Send(data.ToArray(), (packet.IsReliable ? _ice.SendType.reliable : _ice.SendType.unreliable));
        }

        #region Handlers

        [HideInInspector] public UnityEvent Connected;

        [HideInInspector] public UnityEvent Disconnected;

        [HideInInspector] public UnityEvent<byte[], ushort> PacketLost;

        void IClientHandler.Connected()
        {
            Connected?.Invoke();
        }

        void IClientHandler.Disconnected()
        {
            Disconnected?.Invoke();
        }

        void IClientHandler.Handled(byte[] bytes)
        {
            Traffic.Buffer data = new(0);

            data.LoadBytes(bytes);

            Packet packet;

            try { packet = Traffic.GetPacketInstance(ref data); }
            catch (Exception e) { IceLogger.Instance.LogError(e.Message); return; }

            packet.Deserialize(ref data);

            Traffic.InvokeHandler(packet);
        }

        void IClientHandler.PacketLost(byte[] data, ushort id)
        {
            IceLogger.Instance.LogInfo($"RELIABLE PACKET LOST [SIZE: {data.Length}, ID: {id}]");

            PacketLost?.Invoke(data, id);
        }

        #endregion

        private void Release()
        {
            _socket?.Disconnect();
            _socket = null;
        }

        private void OnDestroy() => Release();


    }
}