using System;
using NetMQ;

namespace ReliablePubSub.Common
{
    public interface IConnectionMonitor : IDisposable
    {
        bool IsConnected { get; set; }

        bool TryConnectAndMonitorSocket(NetMQSocket socket, string address, NetMQPoller poller = null,
            Action<IConnectionMonitor, bool> onConnectionStateChanged = null);

        bool WaitForConnection();
    }
}