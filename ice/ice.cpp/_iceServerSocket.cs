using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ICENet.dll
{
    public sealed class _iceServerSocket
    {
        private IntPtr _socket = default;

        private IntPtr _udp = default;

        private IServerHandler _handler;

        #region external

        public delegate void ServerHandleData(IntPtr serv_sock, IntPtr ptr_data, ushort size, IntPtr ptr_connection);

        public delegate bool ServerPredicateAddConnection(IntPtr serv_sock, IntPtr ptr_ep, string address, ushort port);

        public delegate void ServerReliablePacketLost(IntPtr serv_sock, IntPtr ptr_data, ushort size, ushort id, IntPtr ptr_connection);

        public delegate void ServerConnected(IntPtr serv_sock, IntPtr ptr_connection, IntPtr ptr_ep, string address, ushort port);

        public delegate void ServerDisconnected(IntPtr serv_sock, IntPtr ptr_connection);

        private const string _dllPath = "ice_net";    

        [DllImport(_dllPath)]
        private static extern IntPtr create_server();

        [DllImport(_dllPath)]
        private static extern void server_set_transport(IntPtr sock, IntPtr udp);

        [DllImport(_dllPath)]
        private static extern void start_server(IntPtr serv_sock);

        [DllImport(_dllPath)]
        private static extern void update_server(IntPtr serv_sock);

        [DllImport(_dllPath)]
        private static extern void server_set_handle(IntPtr serv_sock, ServerHandleData action);

        [DllImport(_dllPath)]
        private static extern void server_set_predicate_add_connection(IntPtr serv_sock, ServerPredicateAddConnection action);

        [DllImport(_dllPath)]
        private static extern void server_set_reliable_packet_lost(IntPtr serv_sock, ServerReliablePacketLost action);

        [DllImport(_dllPath)]
        private static extern void server_set_connected(IntPtr serv_sock, ServerConnected action);
        [DllImport(_dllPath)]
        private static extern void server_set_disconnected(IntPtr serv_sock, ServerDisconnected action);

        [DllImport(_dllPath)]
        private static extern void server_send_unreliable(IntPtr serv_sock, IntPtr ptr_data, ushort size, IntPtr ptr_ep);
        [DllImport(_dllPath)]
        private static extern void server_send_reliable(IntPtr serv_sock, IntPtr ptr_data, ushort size, IntPtr ptr_ep);

        [DllImport(_dllPath)]
        private static extern void server_remove_connection(IntPtr serv_sock, IntPtr ptr_connection);

        [DllImport(_dllPath)]
        private static extern IntPtr release_server(IntPtr serv_sock);

        #endregion

        private static Dictionary<IntPtr, _iceServerSocket> _instances = new();

        #region connections

        public class connection
        {
            public connection(IntPtr connection, IntPtr ep, string address, ushort port)
            {
                EXT_Connection = connection;
                EXT_EndPoint = ep;
                Address = address;
                Port = port;
            }

            public readonly IntPtr EXT_Connection;

            public readonly IntPtr EXT_EndPoint;

            public readonly string Address;

            public readonly ushort Port;
        }

        private Dictionary<IntPtr, connection> _connections = new();

        #endregion

        public _iceServerSocket(IServerHandler handler)
        {
            _socket = create_server();

            _instances[_socket] = this;
            _instances[_socket]._handler = handler;

            server_set_handle(_socket, (s, d, i, c) => OnHandle(s, d, i, c));
            server_set_predicate_add_connection(_socket, (s, e, n, p) => OnPredicateAddConnection(s, e, n, p));
            server_set_connected(_socket, (s, e, c, a, p) => OnConnected(s, e, c, a, p));
            server_set_disconnected(_socket, (s, c) => OnDisconnected(s, c));
            server_set_reliable_packet_lost(_socket, (s, d, si, i, c) => OnReliablePacketLost(s, d, si, i, c));
        }

        public void Bind(ushort port)
        {
            _udp = _ice.CreateUdp(port);
            server_set_transport(_socket, _udp);
        }

        public void Bind(IntPtr udp)
        {
            _udp = default;
            server_set_transport(_socket, udp);
        }

        public void Start()
        {
            start_server(_socket);
        }

        public void Update()
        {
            update_server(_socket);
        }

        public void Send(connection connection, byte[] data, _ice.SendType sendType)
        {
            IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, dataPtr, data.Count());

            if (sendType == _ice.SendType.unreliable) server_send_unreliable(_socket, dataPtr, (ushort)data.Length, connection.EXT_EndPoint);
            else server_send_reliable(_socket, dataPtr, (ushort)data.Length, connection.EXT_EndPoint);

            Marshal.FreeHGlobal(dataPtr);
        }

        public void Disconnect(connection connection)
        {
            server_remove_connection(_socket, connection.EXT_Connection);
        }

        public void Stop()
        {
            _socket = release_server(_socket);
            _udp = _udp == default ? default : _ice.ReleaseUdp(_udp);
        }

        #region static wrap

        static void OnHandle(IntPtr serv_sock, IntPtr ptr_data, ushort size, IntPtr ptr_connection)
        {
            byte[] data = new byte[size];
            Marshal.Copy(ptr_data, data, 0, size);

            if (_instances[serv_sock]._connections.TryGetValue(ptr_connection, out connection c) == false) return;

            _instances[serv_sock]._handler.Handled(data, c);
        }

        static bool OnPredicateAddConnection(IntPtr serv_sock, IntPtr serv_ep, string address, ushort port)
        {
            return _instances[serv_sock]._handler.PredicateAddConnection(address, port);
        }

        static void OnReliablePacketLost(IntPtr serv_sock, IntPtr ptr_data, ushort size, ushort id, IntPtr ptr_connection)
        {
            byte[] data = new byte[size];
            Marshal.Copy(ptr_data, data, 0, size);

            if (_instances[serv_sock]._connections.TryGetValue(ptr_connection, out connection c) == false) return;

            _instances[serv_sock]._handler.PacketLost(data, id, c);
        }

        static void OnConnected(IntPtr serv_sock, IntPtr ptr_connection, IntPtr serv_ep, string address, ushort port)
        {
            connection connection = new(ptr_connection, serv_ep, address, port);

            _instances[serv_sock]._connections.Add(ptr_connection, connection);

            _instances[serv_sock]._handler.Connected(connection);
        }

        static void OnDisconnected(IntPtr serv_sock, IntPtr ptr_connection)
        {
            connection connection = _instances[serv_sock]._connections[ptr_connection];

            _instances[serv_sock]._connections.Remove(ptr_connection);

            _instances[serv_sock]._handler.Disconnected(connection);
        }

        #endregion
    }

    public interface IServerHandler
    {
        public void Connected(_iceServerSocket.connection connection);

        public void Disconnected(_iceServerSocket.connection connection);

        public void Handled(byte[] data, _iceServerSocket.connection connection);

        public bool PredicateAddConnection(string ip, ushort port);

        public void PacketLost(byte[] data, ushort id, _iceServerSocket.connection connection);
    }
}