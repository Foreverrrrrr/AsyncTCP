using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModbusClient
{
    public partial class Form1 : Form
    {
        ModbusTCPClient modbusTCPClient;
        public Form1()
        {
            InitializeComponent();
            modbusTCPClient = new ModbusTCPClient("127.0.0.1", 502);
            modbusTCPClient.SendEvent += (a, c) =>
            {
                Console.WriteLine("发送：" + a.ToString() + "(" + c + ")");
            };
            modbusTCPClient.ReceiveEvent += (a, c) =>
            {
                Console.WriteLine("接收：" + a.ToString() + "(" + c + ")");
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                modbusTCPClient.Write(ModbusTCPClient.SetFunction.SetReaister, "100", i);
            }
            //Task.Run(() =>
            //{
            //    for (int i = 0; i < 1000; i++)
            //    {
            //        modbusTCPClient.Write(ModbusTCPClient.SetFunction.SetReaister, "100", i);
            //    }
            //});
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }
    }
}
