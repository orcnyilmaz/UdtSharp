using System;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: Client.exe LOCAL_PORT SERVER_ADDRESS SERVER_PORT");
                Console.WriteLine("Example: Client.exe 2020 127.0.0.1 5880");
            }

            UdtClient client = new UdtClient(Convert.ToUInt16(args[0]), args[1],Convert.ToUInt16(args[2]));
            client.Connect();

            Console.WriteLine("Press Any Key To Close Client");
            Console.ReadKey();
        }
    }
}
