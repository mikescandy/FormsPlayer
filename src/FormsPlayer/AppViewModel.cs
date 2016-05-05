using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.AspNet.SignalR.Client;
using Plugin.Settings;

namespace Xamarin.Forms.Player
{
    /// <summary>
    /// Internal view model for the app, that maintains the state 
    /// with the forms player hub.
    /// </summary>
    internal class AppViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        bool isSleepConnected;
        bool isConnected;
        string status;
        string xaml;
        string json;
        string jsonFileName;
        string sessionId;
        string url;
        string port;

        HubConnection connection;

        public AppViewModel()
        {
            ConnectCommand = new Command(Connect);
            DisconnectCommand = new Command(Disconnect);
            SessionId = CrossSettings.Current.GetValueOrDefault<string>("SessionId", "k8mcdd");
            Port = CrossSettings.Current.GetValueOrDefault<string>("Port", "8080");
            Url = CrossSettings.Current.GetValueOrDefault<string>("Url", "http://");
        }

        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                SetProperty(ref isConnected, value, "IsConnected");
                PropertyChanged(this, new PropertyChangedEventArgs("IsDisconnected"));
            }
        }

        public bool IsDisconnected { get { return !isConnected; } }

        public string SessionId { get { return sessionId; } set { SetProperty(ref sessionId, value, "SessionId"); } }

        public string Status { get { return status; } set { SetProperty(ref status, value, "Status"); } }

        public string Xaml { get { return xaml; } set { SetProperty(ref xaml, value, "Xaml"); } }

        public string Json { get { return json; } set { SetProperty(ref json, value, "Json"); } }

        public string JsonFileName { get { return jsonFileName; } set { SetProperty(ref jsonFileName, value, "JsonFileName"); } }

        public ICommand ConnectCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public string SignalrHub => $"{Url}:{Port}";

        public string Url { get { return url; } set { SetProperty(ref url, value, "Url"); } }

        public string Port { get { return port; } set { SetProperty(ref port, value, "Port"); } }

        public void Start()
        {
            // We don't do anything special on start (yet?)
        }

        public void Sleep()
        {
            // Disconnect automatically.
            isSleepConnected = IsConnected;
            if (IsConnected)
                Disconnect();
        }

        public void Resume()
        {
            // Reconnect automatically if it was previously connected.
            if (isSleepConnected)
                Connect();
        }

        void Connect()
        {
            IsConnected = false;
            connection = new HubConnection(SignalrHub);
            var proxy = connection.CreateHubProxy("FormsPlayer");

            proxy.On<string>("Xaml", xaml => Xaml = xaml);
            proxy.On<string[]>("Json", json => { Json = json[1]; JsonFileName = json[0]; });

            try
            {
                connection.Start().Wait(3000);
                proxy.Invoke("Join", SessionId);
                IsConnected = true;
                Status = "Successfully connected to FormsPlayer";
            }
            catch (Exception e)
            {
                var message = e.Message;
                if (e is AggregateException)
                    message = ((AggregateException)e).InnerException.Message;

                Status = "Error connecting to FormsPlayer: " + message;
                connection.Dispose();
            }
        }

        void Disconnect()
        {
            connection.Stop();
            connection.Dispose();
            connection = null;
            IsConnected = false;
        }

        void SetProperty<T>(ref T field, T value, string name)
        {
            if (!Object.Equals(field, value))
            {
                field = value;
                PropertyChanged(this, new PropertyChangedEventArgs(name));
                if (name == "SessionId" || name == "Url" || name == "Port")
                    CrossSettings.Current.AddOrUpdateValue(name, value);
            }
        }
    }
}
