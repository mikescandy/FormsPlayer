using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.PlatformUI;
using PropertyChanged;
using ScandySoft.Forms.Peek.Core;
using WebSocketSharp;
using Xamarin.Forms.Player.Diagnostics;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Thread = System.Threading.Thread;

namespace Xamarin.Forms.Player
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    [ImplementPropertyChanged]
    public class FormsPlayerViewModel : INotifyPropertyChanged
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

        const string SettingsPath = "Xamarin\\FormsPlayer";
        const string SettingsKey = "LastSessionId";
        public string SignalrHub => $"ws://{Url}:{Port}/FormsPeek/{SessionId}";

        static readonly ITracer tracer = Tracer.Get<FormsPlayerViewModel>();
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        private DocumentEvents events;
        private WritableSettingsStore settings;
        private WebSocket connection;

        [ImportingConstructor]
        public FormsPlayerViewModel([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        {
            ConnectCommand = new DelegateCommand(Connect, () => !IsConnected);
            DisconnectCommand = new DelegateCommand(Disconnect, () => IsConnected);
            events = services.GetService<DTE>().Events.DocumentEvents;
            events.DocumentSaved += document => Publish(document.FullName);
            events.DocumentOpened += document => Publish(document.FullName);
            var manager = new ShellSettingsManager(services);
            settings = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!settings.CollectionExists(SettingsPath))
                settings.CreateCollection(SettingsPath);
            if (settings.PropertyExists(SettingsPath, SettingsKey))
                SessionId = settings.GetString(SettingsPath, SettingsKey, "");

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

        void Publish(string fileName)
        {
            if (!IsConnected)
            {
                tracer.Warn("!FormsPlayer is not connected yet.");
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

        void PublishXaml(string fileName)
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
                    tracer.Info("!Publishing XAML payload");
                    connection.Send(xml);
                    //proxy.Invoke("Xaml", SessionId, xml)
                    //	.ContinueWith(t =>
                    //	   tracer.Error(t.Exception.InnerException, "Failed to publish XAML payload."),
                    //		CancellationToken.None,
                    //		TaskContinuationOptions.OnlyOnFaulted,
                    //		TaskScheduler.Default);
                }
            }
            catch (XmlException)
            {
                return;
            }
        }

        void PublishJson(string fileName)
        {
            // Make sure we can read it as XML, just to safeguard the client.
            try
            {
                var json = JObject.Parse(File.ReadAllText(fileName));
                tracer.Info("!Publishing JSON payload");
                connection.Send(json.ToString(Newtonsoft.Json.Formatting.None));
                //proxy.Invoke("Json", SessionId, Path.GetFileName(fileName), json.ToString(Newtonsoft.Json.Formatting.None))
                //	.ContinueWith(t =>
                //	   tracer.Error(t.Exception.InnerException, "Failed to publish JSON payload."),
                //		CancellationToken.None,
                //		TaskContinuationOptions.OnlyOnFaulted,
                //		TaskScheduler.Default);

            }
            catch (JsonException)
            {
                return;
            }
        }

        void Connect()
        {
            IsConnected = false;
            System.Threading.Tasks.Task.Run(() =>
            {
                //connection = new HubConnection(SignalrHub);
                //proxy = connection.CreateHubProxy("FormsPlayer");
                //connection.Open(SignalrHub);
                Log += SignalrHub;
                Log += "\r\n";
                connection = new WebSocket(SignalrHub);
                connection.OnOpen += ConnectionOnOnOpen;
                connection.OnError += ConnectionOnOnError;
                connection.OnMessage += ConnectionOnOnMessage;

                connection.Connect();
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
                connection.Send(json);
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
                    //connection.Dispose();
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

        void Disconnect()
        {
            connection.Close();
            connection.OnOpen -= ConnectionOnOnOpen;
            connection.OnMessage -= ConnectionOnOnMessage;
            connection.OnError -= ConnectionOnOnError;
            IsConnected = false;
        }

        void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            tracer.Error(e.Exception.GetBaseException().InnerException, "Background task exception.");
            e.SetObserved();
        }
    }
}