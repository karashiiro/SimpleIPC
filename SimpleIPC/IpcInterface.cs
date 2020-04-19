using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PeanutButter.SimpleHTTPServer;

namespace SimpleIPC
{
    public class IpcInterface : IDisposable
    {
        public event EventHandler<IpcEventArgs> OnMessageReceived; 

        private readonly bool _httpExternal;
        private readonly HttpClient _client;
        private readonly HttpServer _server;

        /// <summary>
        /// The Action to employ when logging (defaults to logging to the console).
        /// </summary>
        public Action<string> LogAction { get => _server.LogAction; set => _server.LogAction = value; }

        /// <summary>
        /// The address of the partner process's IPC interface.
        /// </summary>
        public Uri PartnerAddress => new Uri($"http://localhost:{PartnerPort}");

        /// <summary>
        /// The port of the partner process's IPC interface.
        /// </summary>
        public int PartnerPort { get; }

        /// <summary>
        /// The address of this interface.
        /// </summary>
        public Uri Address => new Uri($"http://localhost:{Port}");

        /// <summary>
        /// The port of this interface.
        /// </summary>
        public int Port => _server.Port;

        #region Constructors
        /// <summary>
        /// Constructs a new IPC interface using the provided <see cref="HttpClient"/>, port, partner port,
        /// and log action. Leaving the partner port at 0 will set it to the local server's port plus 1.
        /// </summary>
        public IpcInterface(HttpClient client, int port, int partnerPort, Action<string> logAction)
        {
            _client = client;
            _server = new HttpServer(port, logAction);
            _server.AddJsonDocumentHandler((processor, stream) => ReceivedMessage(stream));
            PartnerPort = partnerPort != 0 ? partnerPort : Port + 1;
        }

        /// <summary>
        /// Constructs a new IPC interface using the provided <see cref="HttpClient"/>, port, and partner port.
        /// </summary>
        public IpcInterface(HttpClient client, int port, int partnerPort) : this(client, port, partnerPort, s => {})
        {
            _httpExternal = true;
        }

        /// <summary>
        /// Constructs a new IPC interface using the provided <see cref="HttpClient"/> and port.
        /// </summary>
        public IpcInterface(HttpClient client, int port) : this(client, port, 0, s => {})
        {
            _httpExternal = true;
        }

        /// <summary>
        /// Constructs a new IPC interface using the provided <see cref="HttpClient"/>.
        /// </summary>
        public IpcInterface(HttpClient client) : this(client, 0, 0, s => {})
        {
            _httpExternal = true;
        }

        /// <summary>
        /// Constructs a new IPC interface using the provided port and partner port.
        /// </summary>
        public IpcInterface(int port, int partnerPort) : this(new HttpClient(), port, partnerPort, s => {})
        {
        }

        /// <summary>
        /// Constructs a new IPC interface using the provided port.
        /// </summary>
        public IpcInterface(int port) : this(new HttpClient(), port, 0, s => {})
        {
        }

        /// <summary>
        /// Constructs a new IPC interface using a random port.
        /// </summary>
        public IpcInterface() : this(new HttpClient(), 0, 0, s => {})
        {
        }
        #endregion

        public void On<T>(Action<T> action)
        {
            OnMessageReceived += (sender, e) =>
            {
                T obj;
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(e.SerializedObject);
                }
                catch (JsonReaderException)
                {
                    return;
                }

                action(obj);
            };
        }

        public Task<HttpResponseMessage> SendMessage<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            using var messageBytes = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
            return _client.PostAsync(PartnerAddress, messageBytes);
        }

        private byte[] ReceivedMessage(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var obj = Encoding.UTF8.GetString(memoryStream.GetBuffer());
            InvokeOnMessageReceived(obj);
            return Array.Empty<byte>();
        }

        private void InvokeOnMessageReceived(string obj)
        {
            var handler = OnMessageReceived;
            handler?.Invoke(this, new IpcEventArgs { SerializedObject = obj });
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_httpExternal) _client.Dispose();
                _server.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
