using System;
using System.Collections.Concurrent;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;
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

        public RelayCommand StartCommand { get; set; }
        public RelayCommand SendCommand { get; set; }
        private static WebSocket connection;

        public MainViewModel()
        {
            SessionId = "ksm88cd";
            Port = "8924";
            StartCommand = new RelayCommand(() =>
            {
                if (!Started)
                {
                    wssv = new WebSocketServer($"ws://localhost:{Port}");
                    wssv.AddWebSocketService<FormsPeek>($"/FormsPeek/{SessionId}");
                    wssv.Start();
                    Started = true;
                }
                else
                {
                    wssv.Stop();
                }
            });

            SendCommand = new RelayCommand(() =>
            {
                if (Started)
                {
                    if (connection == null)
                    {
                        connection = new WebSocket($"ws://localhost:{Port}/FormsPeek/{SessionId}");
                        connection.OnMessage += (sender, args) =>
                        {
                            Log += $"{args.Data}\r\n";
                        };
                        connection.Connect();
                    }
                  
                    connection.Send("testtest");

                   
                }
                else
                {
                    wssv.Stop();
                }
            });
            Singleton.Instance.vm = this;
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
        private ConcurrentBag<string> Ids = new ConcurrentBag<string>();



        public FormsPeek() : this(null)
        {

        }

        public FormsPeek(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "anon#";
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            Singleton.Instance.vm.Log += $"{e.Data} \r\n";
             Sessions.Broadcast("suca");
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
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Ids.Add(this.ID);
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
