using System;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using ReliablePubSub.Common;

namespace ReliablePubSub.Client
{
    class SnapshotClient : IDisposable
    {
        private readonly string _address;
        private RequestSocket _socket;
        private readonly TimeSpan _timeout;
        private IConnectionMonitor _monitor;


        public SnapshotClient(TimeSpan timeout, string address)
        {
            _address = address;
            _timeout = timeout;
        }

        public void Connect()
        {
            _socket = new RequestSocket();
            _monitor = new DefaultConnectionMonitor();
            if (!_monitor.TryConnectAndMonitorSocket(_socket, _address))
                throw new ApplicationException($"Couldn't connect to snapshot address {_address} within timeout {_timeout}");
        }

        public bool TryGetSnapshot(string topic, out NetMQMessage snapshot)
        {
            _socket.SendFrame(topic);
            snapshot = new NetMQMessage();
            return _socket.TryReceiveMultipartMessage(_timeout, ref snapshot);
        }

        public void Dispose()
        {
            _monitor?.Dispose();
            _socket?.Dispose();
        }
    }
}
