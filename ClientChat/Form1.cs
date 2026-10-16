using ChatLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientChat
{
    public partial class Form1 : Form
    {
        private readonly ChatClient _chat = new ChatClient(); // instance the library

        public Form1()
        {
            InitializeComponent();
            _chat.MessageReceived += OnMessageReceived;  // SUBSCRIBE to the doorbells
            _chat.Disconnected += OnDisconnected;
        }

        // Network > Connect   (async void is OK only for event handlers)
        private async void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_chat.IsConnected) { AppendLine("Already connected."); return; }
            try
            {
                AppendLine("Connecting...");
                await _chat.ConnectAsync("127.0.0.1", 8080);
                AppendLine("Connected to server.");
            }
            catch (SocketException ex)
            {
                AppendLine("Could not connect: " + ex.Message);
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (!_chat.IsConnected) { AppendLine("Not connected."); return; }
            string text = txtMessage.Text.Trim();
            if (text.Length == 0) return;
            await _chat.SendAsync(text);
            AppendLine(">> " + text);
            txtMessage.Clear();
        }

        // Runs on a BACKGROUND thread -> hop to the UI thread before touching controls.
        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => AppendLine("<< " + e.Message.TrimEnd())));
            else
                AppendLine("<< " + e.Message.TrimEnd());
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            if (InvokeRequired) Invoke(new Action(() => AppendLine("Disconnected.")));
            else AppendLine("Disconnected.");
        }

        // Network > Disconnect
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e) => _chat.Disconnect();

        // File > Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        // Red X and Exit both end up here
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_chat.IsConnected) _chat.Disconnect();
        }

        private void AppendLine(string line) => txtChat.AppendText(line + Environment.NewLine);
    }
}