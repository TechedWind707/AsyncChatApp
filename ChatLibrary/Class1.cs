using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatLibrary
{
    // Carries the message text to whoever subscribed to the event.
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public MessageEventArgs(string message) { Message = message; }
    }

    public class ChatClient
    {
        private TcpClient _client;      // our end of the TCP "phone call"
        private NetworkStream _stream;  // read/write bytes through this

        // EVENTS (doorbells). Declared here, raised here, subscribed in the form.
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler Disconnected;

        public bool IsConnected => _client != null && _client.Connected;

        // Connect WITHOUT freezing the UI.
        public async Task ConnectAsync(string host, int port)
        {
            _client = new TcpClient();
            // throws SocketException if nobody is listening on that port
            await _client.ConnectAsync(host, port).ConfigureAwait(false);
            _stream = _client.GetStream();
            _ = ReceiveLoopAsync();  // start listening in the background (runs until disconnect)
        }

        // text -> bytes -> network
        public async Task SendAsync(string text)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");
            byte[] data = Encoding.ASCII.GetBytes(text + "\n"); // \n so ncat prints a new line
            await _stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        // Wait for bytes -> turn into text -> ring the doorbell. Repeat forever.
        private async Task ReceiveLoopAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    // pauses here until data arrives; the thread is free meanwhile
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (bytesRead == 0) break;  // 0 bytes = the server hung up
                    string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(this, new MessageEventArgs(msg)); // RAISE the event
                }
            }
            catch (IOException) { /* connection dropped mid-read */ }
            catch (ObjectDisposedException) { /* we called Disconnect() */ }
            finally
            {
                Disconnected?.Invoke(this, EventArgs.Empty);  // tell the UI we're done
            }
        }

        public void Disconnect()
        {
            _stream?.Close();  // makes the waiting ReadAsync throw -> loop ends
            _client?.Close();
        }
    }
}