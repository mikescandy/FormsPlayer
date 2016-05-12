using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ScandySoft.Forms.Peek.Core;
using WebSocketSharp;

namespace Xamarin.Forms.Player
{
	class Program
	{
		static readonly TraceSource tracer = new TraceSource("*");

	 
        public static string SessionId;

        public static string Url;

        public static string Port;
	    private static WebSocket connection;
	    public static string SignalrHub => $"ws://{Url}:{Port}/FormsPeek/{SessionId}";
        static void Main (string[] args)
		{
			//Console.WriteLine ("Enter SessionId [ENTER]");
   //         SessionId = Console.ReadLine ().Trim ();
            SessionId = "ksm88cd";
            Port = "8924";
            Url = "localhost";

            Task.Run (((Func<Task>)Startup));
			Console.ReadLine ();
		}

	    private static bool Connected;
		static async Task Startup ()
		{
            connection = new WebSocket(SignalrHub);
            connection.OnOpen += ConnectionOnOnOpen;
            connection.OnError+= ConnectionOnOnError;
            connection.OnMessage += ConnectionOnOnMessage;
          
            connection.Connect();
		    while (!Connected)
		    {
		        Thread.Sleep(100);
		    }
            var client = new Client { Name = "Console", Platform = "Console" };
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            var json = JsonConvert.SerializeObject(client, jsonSerializerSettings);
            connection.Send(json);
            //            var proxy = connection.CreateHubProxy("FormsPlayer");

            //			proxy.On<string> ("Xaml", xaml => tracer.TraceInformation (@"Received XAML: 
            //" + xaml));

            //			proxy.On<string> ("Json", json => tracer.TraceInformation (@"Received JSON: 
            //" + json));

            //			await connection.Start ();
            //			await proxy.Invoke ("Join", sessionId);
        }

        private static void ConnectionOnOnMessage(object sender, MessageEventArgs messageEventArgs)
	    {
	        Console.WriteLine(messageEventArgs.Data);
	    }

	    private static void ConnectionOnOnError(object sender, ErrorEventArgs errorEventArgs)
	    {
            Console.WriteLine(errorEventArgs.Message);

        }

        private static void ConnectionOnOnOpen(object sender, EventArgs eventArgs)
	    {
            Console.WriteLine(eventArgs.ToString());
            Connected = true;


	    }
    }
}
