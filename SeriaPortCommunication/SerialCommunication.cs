using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriaPortCommunication
{
    public class SerialCommunication
    {
        private static SerialPort serialPortseria;
        public static bool IsConnect { get; set; }

        public static SerialCommunication ThisPort { get; set; }
        /// <summary>
        /// 串口数据接收事件
        /// </summary>
        public event Action<DateTime, List<byte>> OnReadEv;

        public SerialCommunication(string comname, int baudrate)
        {
            ThisPort = this;
            serialPortseria = new SerialPort();
            serialPortseria.PortName = comname;
            serialPortseria.BaudRate = baudrate;
            serialPortseria.Parity = Parity.None;
            serialPortseria.StopBits = StopBits.One;
            //serialPortseria.ReceivedBytesThreshold = 9;
            serialPortseria.DataReceived += SerialPort_DataReceived;
            OpenPorts();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            List<byte> buffer = new List<byte>();
            byte[] data = new byte[1024];
            while (true)
            {
                System.Threading.Thread.Sleep(10);
                if (serialPortseria.BytesToRead < 1)
                    break;
                int recCount = serialPortseria.Read(data, 0, Math.Min(serialPortseria.BytesToRead, data.Length));
                byte[] buffer2 = new byte[recCount];
                Array.Copy(data, 0, buffer2, 0, recCount);
                buffer.AddRange(buffer2);
            }
            OnReadEv?.BeginInvoke(DateTime.Now, buffer, null, null);
        }

        private void OpenPorts()
        {
            try
            {
                if (!serialPortseria.IsOpen)
                {
                    serialPortseria.Open();
                    serialPortseria.ReadTimeout = 5000;
                    if (serialPortseria.IsOpen)
                        IsConnect = true;
                    else
                        IsConnect = false;
                }
            }
            catch (Exception ex)
            {
                IsConnect = false;
            }
        }

        public  void Write(string sendmessage)
        {
            try
            {
                if (IsConnect)
                    serialPortseria.Write(sendmessage + "\r\n");
            }
            catch (Exception ex)
            {
                IsConnect = false;
            }
        }

        public void Close()
        {
            IsConnect = false;
            if (serialPortseria != null)
                serialPortseria.Dispose();
        }
    }
}
