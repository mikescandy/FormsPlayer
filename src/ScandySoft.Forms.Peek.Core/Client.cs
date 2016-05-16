namespace ScandySoft.Forms.Peek.Core
{
    public class Client
    {
        public static readonly string Android = "Android";
        public static readonly string Apple = "Apple";
        public static readonly string Console = "Console";
        public static readonly string Visualstudio = "Visualstudio";
        public static readonly string Windows = "Windows";

        public string Id { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
    }
}