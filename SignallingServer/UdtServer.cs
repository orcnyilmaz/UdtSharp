using log4net;
using System;
using System.Net;
using System.Net.Sockets;

namespace SignallingServer
{
    public class UdtServer
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(UdtServer));

        private ushort port;
        private bool started;
        private Udt.Socket socket;

        public UdtServer(ushort port)
        {
            this.port = port;
        }


        public void Start()
        {
            try
            {
                if (started)
                {
                    LOGGER.Error("Already started");
                }

                started = true;
                socket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
                socket.Bind(IPAddress.Any, port);

                LOGGER.Info("server started with port: " + port);
                ListenPeers();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Socket Loop Exception", ex);
                throw ex;
            }
            finally
            {
                started = false;
                ShutdownSocket();
            }
        }

        private void ListenPeers()
        {
            socket.Listen(1);
            while (started)
            {
                LOGGER.Info("waiting for first client connection");
                Udt.Socket client1 = socket.Accept();
                IPEndPoint client1Endpoint = client1.RemoteEndPoint;
                LOGGER.Info("first client connected. IP:" + client1Endpoint.ToString());

                Udt.Socket client2 = socket.Accept();
                IPEndPoint client2Endpoint = client2.RemoteEndPoint;
                LOGGER.Info("second client connected. IP:" + client2Endpoint.ToString());


                LOGGER.Info("sending client1 endpoint to client 2");
                SendAddressTo(client1Endpoint, client2);

                LOGGER.Info("sending client2 endpoint to client 1");
                SendAddressTo(client2Endpoint, client1);

                LOGGER.Info("PEERS CONNECTED");

            }
        }
        private void ShutdownSocket()
        {
            try
            {
                socket.Close();
                socket.Dispose();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Socket close exception", ex);
            }
        }

        private void SendAddressTo(IPEndPoint endPoint, Udt.Socket socket)
        {
            using (Udt.NetworkStream st = new Udt.NetworkStream(socket))
            {
                byte[] addrBytes = endPoint.Address.GetAddressBytes();
                byte[] portBytes = BitConverter.GetBytes(endPoint.Port);

                byte[] endPointInfoArr = new byte[8];
                Array.Copy(addrBytes, 0, endPointInfoArr, 0, addrBytes.Length);

                Array.Copy(portBytes, 0, endPointInfoArr, 4, portBytes.Length);
                st.Write(endPointInfoArr, 0, endPointInfoArr.Length);
            }
        }
    }
}
