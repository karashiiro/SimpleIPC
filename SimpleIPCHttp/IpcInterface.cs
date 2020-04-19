using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SimpleIPCHttp
{
    public class IpcInterface : IDisposable
    {
        public event EventHandler<IpcEventArgs> OnMessageReceived; 

        private readonly bool _httpExternal;
        private readonly HttpClient _client;
        private readonly HttpListener _server;

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
        public int Port { get; }

        #region Constructors
        /// <summary>
        /// Constructs a new IPC interface using the provided <see cref="HttpClient"/>, port, partner port,
        /// and log action. Leaving the partner port at 0 will set it to the local server's port plus 1.
        /// </summary>
        public IpcInterface(HttpClient client, int port, int partnerPort, Action<string> logAction)
        {
            Port = port != 0 ? port : FreeTcpPort();
            PartnerPort = partnerPort != 0 ? partnerPort : Port + 1;

            _client = client;
            _server = new HttpListener();
            _server.Prefixes.Add(Address.AbsoluteUri);
            _server.Start();
            Task.Run(ListenerLoop);
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
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                };
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(e.SerializedObject, settings);
                }
                catch (JsonSerializationException)
                {
                    return;
                }
                action(obj);
            };
        }

        public async Task SendMessage<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            using var messageBytes = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
            // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
            await _client.PostAsync(PartnerAddress, messageBytes).ConfigureAwait(false);
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

        private void ListenerLoop()
        {
            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = _server.GetContext();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                var request = context.Request;
                var resBytes = ReceivedMessage(request.InputStream);
                var response = context.Response;
                response.ContentLength64 = resBytes.Length;
                var resOStream = response.OutputStream;
                resOStream.Write(resBytes, 0, resBytes.Length);
                response.Close();
            }
        }

        // https://stackoverflow.com/a/150974
        private static int FreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_httpExternal) _client.Dispose();
                _server.Close();
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
