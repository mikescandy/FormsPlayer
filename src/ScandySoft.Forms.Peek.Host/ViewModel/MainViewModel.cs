using System;
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

        public MainViewModel()
        {
            StartCommand = new RelayCommand(() =>
            {
                if (!Started)
                {
                    wssv = new WebSocketServer(Port);
                    wssv.AddWebSocketService<FormsPeek>($"/FormsPeek/{SessionId}");
                    wssv.Start();
                }
                else
                {
                    wssv.Stop();
                }
            });
        }
    }

    public class FormsPeek : WebSocketBehavior
    {
        private string _prefix;
        private string _name;
        private static int _number = 0;




        public FormsPeek() : this(null)
        {

        }

        public FormsPeek(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "anon#";
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            Sessions.Broadcast(e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Sessions.Broadcast(String.Format("{0} got logged off...", _name));
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            _name = getName();
            Console.WriteLine("open");
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
