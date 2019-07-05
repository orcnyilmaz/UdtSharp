using System;

namespace SignallingServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: SignallingServer.exe LISTEN_PORT");
                Console.WriteLine("Example: SignallingServer.exe 5880");
            }

            UdtServer server = new UdtServer(Convert.ToUInt16(args[0]));
            server.Start();
        }
    }
}
