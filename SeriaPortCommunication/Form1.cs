using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeriaPortCommunication
{
    public partial class Form1 : Form
    {
        SerialCommunication serialCommunicationserialCommunication;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serialCommunicationserialCommunication = new SerialCommunication("Com1", 115200);
            serialCommunicationserialCommunication.OnReadEv += (t,m) =>
            {
                string msg = Encoding.ASCII.GetString(m.ToArray());
                Action action = () =>
                {
                    richTextBox1.Text += t.ToString() + "-" + msg + "\n";
                    richTextBox1.Focus();
                    richTextBox1.Select(richTextBox1.TextLength, 0);
                    richTextBox1.ScrollToCaret();
                };
                Invoke(action);
            };
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialCommunicationserialCommunication.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            serialCommunicationserialCommunication.Write(textBox1.Text);
        }
    }
}
