using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace AsyncTCP
{
    public class AsyncTcpServer
    {

        /// <summary>
        /// TCP端口数据接收事件
        /// </summary>
        public event Action<DateTime, IPEndPoint, string> OnTCPReadEvent;

        public event Action<DateTime, Exception> DisconnectionEvent;

        public event Action<DateTime, IPEndPoint> SuccessfuConnectEvent;
        private object lockObject = new object();
        private Socket socketCore = null;
        private byte[] buffer = new byte[2048];
        private List<ClientSession> sockets = new List<ClientSession>();

        private bool _isconnet;

        public bool IsCommet
        {
            get { return _isconnet; }
            set
            {
                _isconnet = value;
            }
        }

        public AsyncTcpServer(string ip, int port)
        {
            IPAddress pcip = IPAddress.Parse(ip);
            socketCore = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketCore.Bind(new IPEndPoint(pcip, port));
            socketCore.Listen(1024);
            socketCore.BeginAccept(new AsyncCallback(AsyncAcceptCallback), socketCore);
        }

        /// <summary>
        /// 异步传入的连接申请请求
        /// </summary>
        /// <param name="iar">异步对象</param>
        protected void AsyncAcceptCallback(IAsyncResult iar)
        {
            if (iar.AsyncState is Socket server_socket)
            {
                Socket client = null;
                ClientSession session = new ClientSession();
                try
                {
                    client = server_socket.EndAccept(iar);
                    session.Socket = client;
                    session.EndPoint = (IPEndPoint)client.RemoteEndPoint;
                    IsCommet = true;
                    client.BeginReceive(buffer, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), session);
                    lock (session)
                    {
                        sockets.Add(session);
                    }
                    SuccessfuConnectEvent?.BeginInvoke(DateTime.Now, new IPEndPoint(session.EndPoint.Address, session.EndPoint.Port), null, null);
                }
                catch (ObjectDisposedException)//Server Close
                {
                    IsCommet = false;
                    lock (lockObject)
                    {
                        sockets.Remove(session);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    DisconnectionEvent?.BeginInvoke(DateTime.Now, ex, null, null);
                    IsCommet = false;
                    lock (lockObject)
                    {
                        sockets.Remove(session);
                    }
                    client?.Close();
                }
                server_socket.BeginAccept(new AsyncCallback(AsyncAcceptCallback), server_socket);
            }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            if (ar.AsyncState is ClientSession client)
            {
                string msg = "";
                try
                {
                    int length = client.Socket.EndReceive(ar);
                    if (length == 0)
                    {
                        DisconnectionEvent?.BeginInvoke(DateTime.Now, new Exception($"客户端{client.EndPoint.Address}断开连接"), null, null);
                        client.Socket.Close();
                        lock (lockObject)
                        {
                            sockets.Remove(client);
                        }
                        return;
                    };
                    client.Socket.BeginReceive(buffer, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                    byte[] data = new byte[length];
                    Array.Copy(buffer, 0, data, 0, length);
                    msg = Encoding.UTF8.GetString(data, 0, length); //接收数据
                    OnTCPReadEvent?.BeginInvoke(DateTime.Now, new IPEndPoint(client.EndPoint.Address, client.EndPoint.Port), msg, null, null);
                }
                catch (Exception ex)
                {
                    DisconnectionEvent?.BeginInvoke(DateTime.Now, ex, null, null);
                    lock (lockObject)
                    {
                        if (ex.Message == "远程主机强迫关闭了一个现有的连接。")
                        {
                            IsCommet = false;
                            sockets.Remove(client);
                        }
                    }
                }
            }
        }

        public void AsyncWrite(string ip, string meg)
        {
            var emp = sockets.Find(e => e.EndPoint.Address.ToString() == ip);
            if (emp != null)
            {
                byte[] msgBytes = Encoding.ASCII.GetBytes(meg);
                emp.Socket.BeginSend(msgBytes, 0, msgBytes.Length, SocketFlags.None, null, emp.Socket);
            }
        }

        public void AsyncWrite(string ip, int port, string meg)
        {
            var emp = sockets.Find(e => e.EndPoint.Address.ToString() == ip && e.EndPoint.Port == port);
            if (emp != null)
            {
                byte[] msgBytes = Encoding.ASCII.GetBytes(meg);
                emp.Socket.BeginSend(msgBytes, 0, msgBytes.Length, SocketFlags.None, null, emp.Socket);
            }
        }

        public void CloseTCPServer()
        {
            foreach (var socket in sockets)
            {
                socket?.Socket?.Close();
            }
            IsCommet = false;
            sockets.Clear();
            socketCore.Dispose();
        }
    }

    internal class ClientSession
    {
        public Socket Socket { get; set; }

        public IPEndPoint EndPoint { get; set; }

        public override string ToString() => EndPoint.ToString();
    }
}
