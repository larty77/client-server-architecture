using System;
using System.Runtime.InteropServices;

namespace ICENet.dll
{
    public static class _ice
    {
        private const string _dllPath = "ice_net";

        [DllImport(_dllPath)]
        private static extern IntPtr create_transport(ushort port);

        [DllImport(_dllPath)]
        private static extern IntPtr release_transport(IntPtr udp);

        public enum SendType
        {
            unreliable = 0,
            reliable = 1,
        }

        public static IntPtr CreateUdp(ushort port) => create_transport(port);

        public static IntPtr ReleaseUdp(IntPtr udp) => release_transport(udp);
    }
}