using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using ScandySoft.Forms.Peek.Core;
using WebSocketSharp;
using Xamarin.Forms.Player;
using Xamarin.Forms.Player.Diagnostics;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Thread = System.Threading.Thread;

namespace ScandySoft.Forms.Peek
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    [ImplementPropertyChanged]
    public class FormsPeekViewModel : INotifyPropertyChanged
    {
        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public int Clients { get; set; }
        public bool IsConnected { get; set; }
        public string Log { get; set; }
        public bool IsConnecting { get; set; }
        public string SessionId { get; set; }
        public string Url { get; set; }
        public string Port { get; set; }
        public string Status { get; set; }

        private const string SettingsPath = "Xamarin\\FormsPlayer";
        private const string SettingsKey = "LastSessionId";
        public string SignalrHub => $"ws://{Url}:{Port}/FormsPeek/{SessionId}";

        private static readonly ITracer Tracer = Xamarin.Forms.Player.Diagnostics.Tracer.Get<FormsPeekViewModel>();
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        private readonly DocumentEvents _events;
        private readonly WritableSettingsStore _settings;
        private WebSocket _connection;

        [ImportingConstructor]
        public FormsPeekViewModel([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        {
            ConnectCommand = new DelegateCommand(Connect, () => !IsConnected);
            DisconnectCommand = new DelegateCommand(Disconnect, () => IsConnected);
            _events = services.GetService<DTE>().Events.DocumentEvents;
            _events.DocumentSaved += document => Publish(document.FullName);
            _events.DocumentOpened += document => Publish(document.FullName);
            var manager = new ShellSettingsManager(services);
            _settings = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!_settings.CollectionExists(SettingsPath))
                _settings.CreateCollection(SettingsPath);
            if (_settings.PropertyExists(SettingsPath, SettingsKey))
                SessionId = _settings.GetString(SettingsPath, SettingsKey, "");

            if (string.IsNullOrEmpty(SessionId))
            {
                // Initialize SessionId from MAC address.
                var mac = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .First();

                SessionId = NaiveBijective.Encode(NaiveBijective.Decode(mac));
            }
            TaskScheduler.UnobservedTaskException += OnTaskException;
        }

        private void Publish(string fileName)
        {
            if (!IsConnected)
            {
                Tracer.Warn("!FormsPlayer is not connected yet.");
                return;
            }

            if (Path.GetExtension(fileName) == ".xaml")
            {
                PublishXaml(fileName);
            }
            else if (Path.GetExtension(fileName) == ".json")
            {
                PublishJson(fileName);
            }
        }

        private void PublishXaml(string fileName)
        {
            // Make sure we can read it as XML, just to safeguard the client.
            try
            {
                using (var reader = XmlReader.Create(fileName))
                {
                    var xdoc = XDocument.Load(reader);
                    // Strip the x:Class attribute since it doesn't make
                    // sense for the deserialization and might break stuff.
                    var xclass = xdoc.Root.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Class");
                    if (xclass != null)
                        xclass.Remove();
                    xclass = xdoc.Root.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Class");
                    if (xclass != null)
                        xclass.Remove();

                    var xml = xdoc.ToString(SaveOptions.DisableFormatting);
                    Tracer.Info("!Publishing XAML payload");
                    _connection.Send(xml);
                }
            }
            catch (XmlException)
            {
                return;
            }
        }

        private void PublishJson(string fileName)
        {
            // Make sure we can read it as XML, just to safeguard the client.
            try
            {
                var json = JObject.Parse(File.ReadAllText(fileName));
                Tracer.Info("!Publishing JSON payload");
                _connection.Send(json.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (JsonException)
            {
                return;
            }
        }

        private void Connect()
        {
            IsConnected = false;
            System.Threading.Tasks.Task.Run(() =>
            {
                Log += SignalrHub;
                Log += "\r\n";
                _connection = new WebSocket(SignalrHub);
                _connection.OnOpen += ConnectionOnOnOpen;
                _connection.OnError += ConnectionOnOnError;
                _connection.OnMessage += ConnectionOnOnMessage;

                _connection.Connect();
                while (!Connected)
                {
                    Thread.Sleep(100);
                }
                var client = new Client { Name = "VisualStudio", Platform = Client.Visualstudio };
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                };

                var json = JsonConvert.SerializeObject(client, jsonSerializerSettings);
                _connection.Send(json);
                try
                {
                    IsConnected = true;
                    Status = "Successfully connected to FormsPlayer";
                    Log += Status;
                    Log += "\r\n";
                }
                catch (Exception e)
                {
                    Status = "Error connecting to FormsPlayer: " + e.Message;
                    Log += Status;
                    Log += "\r\n";
                }
                finally
                {
                    IsConnecting = false;
                }
            });
        }

        private void ConnectionOnOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
        }

        private void ConnectionOnOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            Connected = false;
        }

        private void ConnectionOnOnOpen(object sender, EventArgs eventArgs)
        {
            Connected = true;
        }

        public bool Connected { get; set; }

        private void Disconnect()
        {
            _connection.Close();
            _connection.OnOpen -= ConnectionOnOnOpen;
            _connection.OnMessage -= ConnectionOnOnMessage;
            _connection.OnError -= ConnectionOnOnError;
            IsConnected = false;
        }

        private void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Tracer.Error(e.Exception.GetBaseException().InnerException, "Background task exception.");
            e.SetObserved();
        }
    }
}