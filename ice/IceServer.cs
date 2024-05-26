using UnityEngine;
using UnityEngine.Events;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System;

using ICENet.dll;
using ICENet.Traffic;

namespace ICENet
{
    public class IceServer : MonoBehaviour, IServerHandler
    {
        private _iceServerSocket _socket;

        public class Connection
        {
            public readonly _iceServerSocket.connection Socket;

            public string Address => Socket.Address;

            public int Port => Socket.Port;

            public Connection(_iceServerSocket.connection connection)
            {
                Socket = connection;
            }
        }

        public readonly Dictionary<_iceServerSocket.connection, Connection> _connections = new();

        public class TrafficHelper
        {
            private readonly PacketFactory _packetFactory;

            private readonly ConcurrentDictionary<Type, Action<Packet, Connection>> _handlers = new();

            private readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new();

            public TrafficHelper()
            {
                _packetFactory = new PacketFactory();
            }

            public void AddOrUpdateHandler<T>(Action<T, Connection> handleAction) where T : Packet, new()
            {
                if (_packetFactory.AddOrUpdate<T>() is true)
                    _handlers[typeof(T)] = (packet, c) => handleAction((T)packet, c);
            }

            internal Packet GetPacketInstance(ref Traffic.Buffer data) => _packetFactory.GetInstance(ref data);

            internal void InvokeHandler(Packet packet, Connection connection)
            {
                try
                {
                    Type packetType = packet.GetType();

                    if (_methodCache.TryGetValue(packetType, out MethodInfo method) is false)
                        _methodCache[packetType] = GetType().GetMethod(nameof(InvokeHandlerGeneric), BindingFlags.Instance |
                            BindingFlags.NonPublic).MakeGenericMethod(packetType);

                    method = _methodCache[packetType];

                    method.Invoke(this, new object[] { packet, connection });
                }
                catch {  }
            }

            private void InvokeHandlerGeneric<T>(T packet, Connection connection) where T : Packet, new()
            {
                try { _handlers[packet!.GetType()].Invoke(packet, connection); } catch { }
            }
        }

        public TrafficHelper Traffic => _traffic;

        private TrafficHelper _traffic = new TrafficHelper();

        public Func<string, ushort, bool> PredicateAddConnection;

        public void Connect(ushort port)
        {
            _socket = new _iceServerSocket(GetComponent<IceServer>());
            _socket.Bind(port);
            _socket.Start();
        }

        private void Update()
        {
            float interval = 1.0f / 1000.0f;
            float last = 0.0f;

            last += Time.deltaTime;

            while (last >= interval)
            {
                _socket?.Update();

                last -= interval;
            }
        }

        public void SendTo(Packet packet, Connection connection)
        {
            Traffic.Buffer data = new(0);

            packet.Serialize(ref data);

            _socket.Send(connection.Socket, data.ToArray(), (packet.IsReliable ? _ice.SendType.reliable : _ice.SendType.unreliable));
        }

        public void SendToAll(Packet packet)
        {
            Traffic.Buffer data = new(0);

            packet.Serialize(ref data);

            foreach(var c in _connections) _socket.Send(c.Value.Socket, data.ToArray(), (packet.IsReliable ? _ice.SendType.reliable : _ice.SendType.unreliable));
        }
        
        public void SendToAllExepct(Packet packet, Connection connection)
        {
            Traffic.Buffer data = new(0);

            packet.Serialize(ref data);

            foreach (var element in _connections)
            {
                if (element.Value == connection) continue;

                _socket.Send(element.Value.Socket, data.ToArray(), (packet.IsReliable ? _ice.SendType.reliable : _ice.SendType.unreliable));
            }
        }

        #region Handlers

        [HideInInspector] public UnityEvent<Connection> ConnectionAdded;

        [HideInInspector] public UnityEvent<Connection> ConnectionRemoved;

        [HideInInspector] public UnityEvent<byte[], ushort, Connection> PacketLost;

        void IServerHandler.Connected(_iceServerSocket.connection socket)
        {
            var connection = new Connection(socket);
            _connections.Add(socket, connection);
            ConnectionAdded?.Invoke(connection);
        }

        void IServerHandler.Disconnected(_iceServerSocket.connection socket)
        {
            var connection = _connections[socket];
            _connections.Remove(connection.Socket);
            ConnectionRemoved?.Invoke(connection);
        }

        void IServerHandler.Handled(byte[] bytes, _iceServerSocket.connection socket)
        {
            Traffic.Buffer data = new(0);

            data.LoadBytes(bytes);

            Packet packet;

            try { packet = Traffic.GetPacketInstance(ref data); }
            catch(Exception e) { IceLogger.Instance.LogError(e.Message); return; }

            packet.Deserialize(ref data);

            Traffic.InvokeHandler(packet, _connections[socket]);
        }

        bool IServerHandler.PredicateAddConnection(string ip, ushort port)
        {
            return PredicateAddConnection.Invoke(ip, port);
        }

        void IServerHandler.PacketLost(byte[] data, ushort id, _iceServerSocket.connection connection)
        {
            IceLogger.Instance.LogInfo($"RELIABLE PACKET LOST [SIZE: {data.Length}, ID: {id}] <===> [{connection.Port}]");

            PacketLost?.Invoke(data, id, _connections[connection]);
        }

        #endregion

        private void Release()
        {
            _socket?.Stop();
            _socket = null;
        }

        private void OnDestroy() => Release();
    }
}