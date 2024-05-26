using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ICENet.dll
{
    public sealed class _iceClientSocket
    {
        private IntPtr _socket = default;

        private IntPtr _udp = default;

        private IClientHandler _handler;

        #region external

        public delegate void ClientHandleData(IntPtr clnt_sock, IntPtr ptr_data, ushort size);

        public delegate void ClientReliablePacketLost(IntPtr serv_sock, IntPtr ptr_data, ushort size, ushort id);

        public delegate void ClientConnected(IntPtr clnt_sock);

        public delegate void ClientDisconnected(IntPtr clnt_sock);

        private const string _dllPath = "ice_net";

        [DllImport(_dllPath)]
        private static extern IntPtr create_client();

        [DllImport(_dllPath)]
        private static extern void client_set_transport(IntPtr sock, IntPtr udp);

        [DllImport(_dllPath)]
        private static extern void start_client(IntPtr clnt_sock, string address, ushort port);

        [DllImport(_dllPath)]
        private static extern void update_client(IntPtr clnt_sock);

        [DllImport(_dllPath)]
        private static extern void client_set_handle(IntPtr clnt_sock, ClientHandleData action);

        [DllImport(_dllPath)]
        private static extern void client_set_reliable_packet_lost(IntPtr serv_sock, ClientReliablePacketLost action);

        [DllImport(_dllPath)]
        private static extern void client_set_connected(IntPtr clnt_sock, ClientConnected action);
        [DllImport(_dllPath)]
        private static extern void client_set_disconnected(IntPtr clnt_sock, ClientDisconnected action);

        [DllImport(_dllPath)]
        private static extern void client_send_unreliable(IntPtr clnt_sock, IntPtr ptr_data, ushort size);
        [DllImport(_dllPath)]
        private static extern void client_send_reliable(IntPtr clnt_sock, IntPtr ptr_data, ushort size);

        [DllImport(_dllPath)]
        private static extern IntPtr release_client(IntPtr clnt_sock);

        #endregion

        private static Dictionary<IntPtr, _iceClientSocket> _instances = new();

        public _iceClientSocket(IClientHandler handler)
        {
            _socket = create_client();

            _instances[_socket] = this;
            _instances[_socket]._handler = handler;

            client_set_handle(_socket, (s, d, i) => OnHandle(s, d, i));
            client_set_connected(_socket, (s) => OnConnected(s));
            client_set_disconnected(_socket, (s) => OnDisconnected(s));
            client_set_reliable_packet_lost(_socket, (s, d, si, i) => OnReliablePacketLost(s, d, si, i));
        }

        public void Bind(ushort port)
        {
            _udp = _ice.CreateUdp(port);
            client_set_transport(_socket, _udp);
        }

        public void Bind(IntPtr udp)
        {
            _udp = default;
            client_set_transport(_socket, udp);
        }

        public void Connect(string address, ushort port)
        {
            start_client(_socket, address, port);
        }

        public void Update()
        {
            update_client(_socket);
        }

        public void Send(byte[] data, _ice.SendType sendType)
        {
            IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, dataPtr, data.Count());

            if (sendType == _ice.SendType.unreliable) client_send_unreliable(_socket, dataPtr, (ushort)data.Length);
            else client_send_reliable(_socket, dataPtr, (ushort)data.Length);

            Marshal.FreeHGlobal(dataPtr);
        }

        public void Disconnect()
        {
            _socket = release_client(_socket);
            _udp = _udp == default ? default : _ice.ReleaseUdp(_udp);
        }

        #region static wrap

        static void OnHandle(IntPtr clnt_sock, IntPtr ptr_data, ushort size)
        {
            byte[] data = new byte[size];
            Marshal.Copy(ptr_data, data, 0, size);

            _instances[clnt_sock]._handler.Handled(data);
        }

        static void OnReliablePacketLost(IntPtr serv_sock, IntPtr ptr_data, ushort size, ushort id)
        {
            byte[] data = new byte[size];
            Marshal.Copy(ptr_data, data, 0, size);

            _instances[serv_sock]._handler.PacketLost(data, id);
        }

        static void OnConnected(IntPtr clnt_sock)
        {
            _instances[clnt_sock]._handler.Connected();
        }

        static void OnDisconnected(IntPtr clnt_sock)
        {
            _instances[clnt_sock]._handler.Disconnected();
        }

        #endregion
    }

    public interface IClientHandler
    { 
        public void Connected();

        public void Disconnected();

        public void Handled(byte[] data);

        public void PacketLost(byte[] data, ushort id);
    }
}