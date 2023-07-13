using AsyncTCP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo
{
    public partial class Form1 : Form
    {
        private AsyncTcpServer tcpServer;
        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            tcpServer = new AsyncTcpServer(textBox1.Text, Convert.ToInt32(textBox2.Text));
            tcpServer.OnTCPReadEvent += (t, i,s) =>
            {
                Action action = () =>
                {
                    richTextBox1.Text += t.ToString() + "-"+i.Address.ToString()+":"+i.Port+"-->" + s + "\n";
                    richTextBox1.Focus();
                    richTextBox1.Select(richTextBox1.TextLength, 0);
                    richTextBox1.ScrollToCaret();
                };
                Invoke(action);
            };
            tcpServer.SuccessfuConnectEvent += (t, s) =>
            {
                Action action = () =>
                {
                    richTextBox1.Text += t.ToString() + "-->" + s.Address.ToString() + ":" + s.Port + "\n";
                    richTextBox1.Focus();
                    richTextBox1.Select(richTextBox1.TextLength, 0);
                    richTextBox1.ScrollToCaret();
                };
                Invoke(action);
            };
            tcpServer.DisconnectionEvent += (t, s) =>
            {
                Action action = () =>
                {
                    richTextBox1.Text += t.ToString() + "-->" + s.StackTrace + s.Message + "\n";
                    richTextBox1.Focus();
                    richTextBox1.Select(richTextBox1.TextLength, 0);
                    richTextBox1.ScrollToCaret();
                };
                Invoke(action);
            };
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(tcpServer.IsCommet)
            {
                tcpServer.AsyncWrite(textBox4.Text, Convert.ToInt32(textBox3.Text), textBox5.Text);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }
    }
}
