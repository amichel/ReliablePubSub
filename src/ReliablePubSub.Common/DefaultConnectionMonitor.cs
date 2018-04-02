using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Monitoring;

namespace ReliablePubSub.Common
{
    public class DefaultConnectionMonitor : IConnectionMonitor
    {
        private readonly int _connectTimeout;
        private volatile bool _isConnected;
        private Action<IConnectionMonitor, bool> _onConnectionStateChanged;
        private NetMQMonitor _monitor;
        private Task _monitorTask;
        private readonly ManualResetEventSlim _connectHandle = new ManualResetEventSlim();

        public DefaultConnectionMonitor(int connectTimeout = 10000)
        {
            _connectTimeout = connectTimeout;
        }

        public bool TryConnectAndMonitorSocket(NetMQSocket socket, string address, NetMQPoller poller = null, Action<IConnectionMonitor, bool> onConnectionStateChanged = null)
        {
            _onConnectionStateChanged = onConnectionStateChanged;

            _monitor = new NetMQMonitor(socket, $"inproc://monitor.socket/{Guid.NewGuid()}", SocketEvents.Connected | SocketEvents.Disconnected);
            _monitor.Connected += MonitorConnected;
            _monitor.Disconnected += MonitorDisconnected;

            if (poller == null)
                _monitorTask = _monitor.StartAsync();
            else
                _monitor.AttachToPoller(poller);

            socket.Connect(address);
            return WaitForConnection();
        }
        
        public bool WaitForConnection()
        {
            return _connectHandle.Wait(_connectTimeout);
        }

        private void MonitorConnected(object sender, NetMQMonitorSocketEventArgs e)
        {
            try
            {
                IsConnected = true;
                _connectHandle.Set();
            }
            catch
            {
                // ignored to not crash monitor
            }
        }

        private void MonitorDisconnected(object sender, NetMQMonitorSocketEventArgs e)
        {
            try
            {
                IsConnected = false;
                _connectHandle.Reset();
            }
            catch
            {
                // ignored to not crash monitor
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                _onConnectionStateChanged?.Invoke(this, value);
            }
        }

        public void Dispose()
        {
            _monitor?.Dispose();
            _monitorTask?.Wait(1000);
        }
    }
}