using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using PropertyChanged;
using ScandySoft.Forms.Peek.Core;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ScandySoft.Forms.Peek.Host.ViewModel
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private WebSocketServer wssv;

        public string Port { get; set; }
        public string SessionId { get; set; }
        public bool Started { get; set; }
        public string Log { get; set; }
        public string ButtonText { get; set; }
        public RelayCommand StartCommand { get; set; }
        public RelayCommand ShowAboutCommand { get; set; }
        public ObservableCollection<Client> Clients { get; set; }

        private static WebSocket connection;

        public MainViewModel()
        {
            Singleton.Instance.vm = this;
            Clients = new ObservableCollection<Client>();
            ButtonText = "Start";
            SessionId = "ksm88cd";
            Port = "8924";
            StartCommand = new RelayCommand(StartRelayCommand);
        }

        private void StartRelayCommand()
        {
            if (!Started)
            {
                wssv = new WebSocketServer($"ws://localhost:{Port}");
                wssv.AddWebSocketService<FormsPeek>($"/FormsPeek/{SessionId}");
                wssv.Start();
                Started = true;
                ButtonText = "Stop";
            }
            else
            {
                wssv.RemoveWebSocketService($"/FormsPeek/{SessionId}");
                wssv.Stop();
                Clients.Clear();
                Started = false;
                ButtonText = "Start";
            }
        }
    }

    public sealed class Singleton
    {
        public MainViewModel vm;

        private static readonly Lazy<Singleton> lazy =
            new Lazy<Singleton>(() => new Singleton());

        public static Singleton Instance { get { return lazy.Value; } }

        private Singleton()
        {
        }
    }



    public class FormsPeek : WebSocketBehavior
    {
        private string _prefix;
        private string _name;
        private static int _number = 0;
        private Client client;



        public FormsPeek() : this(null)
        {

        }

        public FormsPeek(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "client#";
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            Singleton.Instance.vm.Log += $"{e.Data} \r\n";
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var data = JsonConvert.DeserializeObject(e.Data, settings);
            if (data is Client)
            {
                client = data as Client;
                client.Id = _name;
                Application.Current.Dispatcher.Invoke(new Action(() => { Singleton.Instance.vm.Clients.Add(client); }));

            }
            //foreach (var id in Ids)
            //{
            //    Sessions.SendTo(e.Data,id);
            //}
            //foreach (var webSocketSession in Sessions.Sessions)
            //{
            //    Send(e);
            //}
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Sessions.Broadcast(String.Format("{0} got logged off...", _name));
            Application.Current.Dispatcher.Invoke(new Action(() => { Singleton.Instance.vm.Clients.Remove(client); }));

        }

        protected override void OnOpen()
        {
            base.OnOpen();

            _name = getName();
            Singleton.Instance.vm.Log += $"{_name} connected \r\n";



        }
        private string getName()
        {
            var name = Context.QueryString["name"];
            return !name.IsNullOrEmpty() ? name : _prefix + getNumber();
        }

        private static int getNumber()
        {
            return Interlocked.Increment(ref _number);
        }

    }

}
