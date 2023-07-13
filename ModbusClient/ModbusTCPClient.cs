using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static ModbusClient.ModbusTCPClient;

namespace ModbusClient
{
    public class ModbusTCPClient
    {
        public string Target_IP { get; set; }

        public int Target_Port { get; set; }

        public bool IsConnect { get; set; }

        public byte[] ReadBuffer { get; set; } = new byte[1024 * 1024];

        /// <summary>
        /// 接收事件
        /// </summary>
        public event Action<DateTime, string> ReceiveEvent;
        /// <summary>
        /// 发送事件
        /// </summary>
        public event Action<DateTime, string> SendEvent;

        public event Action<DateTime, Exception> DisconnectionEvent;

        public event Action<DateTime, IPAddress> SuccessfuConnectEvent;

        private ushort GetWriteToken = 0;

        public enum GetFunction
        {
            /// <summary>
            /// 读取输出线圈
            /// </summary>
            GetOutCoil = 0x01,
            /// <summary>
            /// 读取输入线圈
            /// </summary>
            GetInputCoil = 0x02,
            /// <summary>
            /// 读取保持寄存器
            /// </summary>
            GetHoldingRegister = 0x03,
            /// <summary>
            /// 读取输入寄存器
            /// </summary>
            GetIputReaister = 0x04,
        }

        public enum SetFunction
        {
            /// <summary>
            /// 写入单线圈
            /// </summary>
            SetCoil = 0x05,
            /// <summary>
            /// 写入单寄存器
            /// </summary>
            SetReaister = 0x06,
            /// <summary>
            /// 写入多线圈
            /// </summary>
            SetCoils = 0x0f,
            /// <summary>
            /// 写入多寄存器
            /// </summary>
            SetReaisters = 0x10,

        }

        public enum TransferFormat
        {
            ABCD,
            BADC,
            CDAB,
            DCBA
        }

        public TransferFormat Format { get; set; } = TransferFormat.CDAB;

        private System.Net.Sockets.TcpClient tcpClient { get; set; }

        public struct SendStructure
        {
            public byte[] Send { get; set; }
            public byte[] Checkout { get; set; }

        }
        public ModbusTCPClient(string targetip, int targetport)
        {
            AsyncNewTcp(targetip, targetport);
        }

        private void AsyncNewTcp(string targetip, int targetport)
        {
            this.Target_IP = targetip;
            this.Target_Port = targetport;
            try
            {
                tcpClient = new System.Net.Sockets.TcpClient();
                tcpClient.BeginConnect(IPAddress.Parse(Target_IP), Target_Port, new AsyncCallback(AsyncConnect), tcpClient);
            }
            catch (Exception ex)
            {
                DisconnectionEvent?.BeginInvoke(DateTime.Now, ex, null, null);
                throw new Exception(ex.Message + "\r" + ex.StackTrace);
            }
        }

        private void AsyncConnect(IAsyncResult async)
        {
            async.AsyncWaitHandle.WaitOne(3000);
            if (!tcpClient.Connected)
            {
                IsConnect = false;
                tcpClient.Close();
                tcpClient = null;
                AsyncNewTcp(Target_IP, Target_Port);
            }
            else
            {
                try
                {
                    IsConnect = true;
                    SuccessfuConnectEvent?.BeginInvoke(DateTime.Now, IPAddress.Parse(Target_IP), null, null);
                    tcpClient.EndConnect(async);
                    // tcpClient.GetStream().BeginRead(ReadBuffer, 0, ReadBuffer.Length, new AsyncCallback(AsyncRead), tcpClient);
                }
                catch (Exception ex)
                {
                    DisconnectionEvent?.BeginInvoke(DateTime.Now, ex, null, null);
                    IsConnect = false;
                    tcpClient.Close();
                    tcpClient = null;
                    AsyncNewTcp(Target_IP, Target_Port);
                }
            }
        }

        private void AsyncRead(IAsyncResult async)
        {
            try
            {
                int len = tcpClient.GetStream().EndRead(async);
                if (len > 0)
                {
                    IsConnect = true;
                    string str = Encoding.ASCII.GetString(ReadBuffer, 0, len);
                    str = Uri.UnescapeDataString(str);
                    ReceiveEvent?.BeginInvoke(DateTime.Now, str, null, null);
                    tcpClient.GetStream().BeginRead(ReadBuffer, 0, ReadBuffer.Length, new AsyncCallback(AsyncRead), tcpClient);
                }
                else
                {
                    throw new Exception("监测到服务器关闭");
                }
            }
            catch (Exception ex)
            {
                DisconnectionEvent?.BeginInvoke(DateTime.Now, ex, null, null);
                IsConnect = false;
                tcpClient.Close();
                tcpClient = null;
                AsyncNewTcp(Target_IP, Target_Port);
            }
        }

        public void SendMessage(string msg, Encoding encoding)
        {
            byte[] msgBytes = encoding.GetBytes(msg);
            tcpClient.GetStream().BeginWrite(msgBytes, 0, msgBytes.Length, (ar) =>
            {
                tcpClient.GetStream().BeginRead(ReadBuffer, 0, ReadBuffer.Length, new AsyncCallback(AsyncRead), tcpClient);
                tcpClient.GetStream().EndWrite(ar);
            }, null);
        }

        public unsafe void Write<T>(SetFunction functioncode, string RegisterAddress, T t) where T : struct
        {
            byte[] receive = new byte[255];
            SendStructure send = new SendStructure();
            if (t.GetType() == typeof(bool))
            {
                Console.WriteLine();
            }
            else if (t.GetType() == typeof(int))
            {
                send = SendInt(RegisterAddress, Convert.ToInt32(t), GetWriteToken);
            }
            else if (t.GetType() == typeof(ushort))
            {
                Console.WriteLine();
            }
            else if (t.GetType() == typeof(float))
            {
                Console.WriteLine();
            }
            else if (t.GetType() == typeof(double))
            {
                Console.WriteLine();
            }
            SendEvent?.Invoke(DateTime.Now, BitConverter.ToString(send.Send, 0, send.Send.Length));
            tcpClient.GetStream().Write(send.Send, 0, send.Send.Length);
            int len = tcpClient.GetStream().Read(receive, 0, receive.Length);
            lock (this)
            {
                if (GetWriteToken < ushort.MaxValue)
                    GetWriteToken++;
                else
                    GetWriteToken = 0;
            }
            if (len > 0)
            {
                if (receive[0] == send.Checkout[0] && receive[1] == send.Checkout[1])
                    ReceiveEvent?.Invoke(DateTime.Now, BitConverter.ToString(receive, 0, len));
            }
        }

        private SendStructure SendInt(string RegisterAddress, int value, int lockint)
        {
            SendStructure sendStructure = new SendStructure();
            var tobyte = String_Byte(RegisterAddress, "x4");
            byte[] counter = BitConverter.GetBytes(lockint);
            byte[] valueby = String_Byte(value.ToString(), "x8", Format);
            byte[] masthead = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
            masthead[0] = counter[0];
            masthead[1] = counter[1];
            byte[] bytes = new byte[] { 0x00, 0x0b, 0x01, (byte)SetFunction.SetReaisters, tobyte[0], tobyte[1], 0x00, 0x02, 0x04, valueby[0], valueby[1], valueby[2], valueby[3] };
            byte[] send = new byte[masthead.Length + bytes.Length];
            Array.Copy(masthead, 0, send, 0, masthead.Length);
            Array.Copy(bytes, 0, send, masthead.Length, bytes.Length);
            sendStructure.Send = send;
            sendStructure.Checkout = counter;
            return sendStructure;
        }

        internal static byte[] String_Byte(string bytestr, string bytelength)
        {
            try
            {
                int d = Convert.ToInt32(bytestr);
                char[] t = d.ToString(bytelength).ToArray(); //X4
                string[] strings = new string[t.Length / 2];
                int k = 0;
                for (int i = 0; i < t.Length; i += 2)
                {
                    if (i + 1 < t.Length)
                    {
                        strings[k] = "0x" + t[i].ToString() + t[i + 1].ToString();

                    }
                    else
                    {
                        strings[k] = "0x" + t[i].ToString();
                    }
                    k++;
                }
                byte[] bytes = new byte[strings.Length];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(strings[i], 16);
                return bytes;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        internal static byte[] String_Byte(string bytestr, string bytelength, TransferFormat transfer)
        {
            try
            {
                int d = Convert.ToInt32(bytestr);
                char[] t = d.ToString(bytelength).ToArray(); //X4
                string[] strings = new string[t.Length / 2];
                int k = 0;
                for (int i = 0; i < t.Length; i += 2)
                {
                    if (i + 1 < t.Length)
                    {
                        strings[k] = "0x" + t[i].ToString() + t[i + 1].ToString();

                    }
                    else
                    {
                        strings[k] = "0x" + t[i].ToString();
                    }
                    k++;
                }
                byte[] bytes = new byte[strings.Length];
                if (transfer == TransferFormat.ABCD)
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = Convert.ToByte(strings[i], 16);
                else if (transfer == TransferFormat.BADC)
                {
                    bytes[0] = Convert.ToByte(strings[1], 16);
                    bytes[1] = Convert.ToByte(strings[0], 16);
                    bytes[2] = Convert.ToByte(strings[3], 16);
                    bytes[3] = Convert.ToByte(strings[2], 16);
                }
                else if (transfer == TransferFormat.CDAB)
                {
                    bytes[0] = Convert.ToByte(strings[2], 16);
                    bytes[1] = Convert.ToByte(strings[3], 16);
                    bytes[2] = Convert.ToByte(strings[0], 16);
                    bytes[3] = Convert.ToByte(strings[1], 16);
                }
                else if (transfer == TransferFormat.DCBA)
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = Convert.ToByte(strings[bytes.Length - i - 1], 16);
                return bytes;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Close()
        {
            if (tcpClient != null && tcpClient.Client.Connected)
                tcpClient.Close();
            if (!tcpClient.Client.Connected)
            {
                tcpClient.Close();
            }
            tcpClient.Dispose();
        }
    }
}
