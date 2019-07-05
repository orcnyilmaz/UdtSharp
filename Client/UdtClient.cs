using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using Udt;

namespace Client
{
    public class UdtClient
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(UdtClient));
        private ushort localPort;
        private string serverAddressStr;
        private ushort serverPort;
        private bool started;
        private Udt.Socket socket;

        public UdtClient(ushort localPort, string serverAddr, ushort serverPort)
        {
            this.localPort = localPort;
            this.serverAddressStr = serverAddr;
            this.serverPort = serverPort;
        }

        public void Connect()
        {
            started = true;
            try
            {
                LOGGER.Info("creating socket");
                socket = CreateSocket();
                LOGGER.Info("connecting to signalling server, address: " + serverAddressStr + " port: " + serverPort);
                IPAddress serverAddress = IPAddress.Parse(serverAddressStr);
                socket.Connect(serverAddress, serverPort);
                LOGGER.Info("connected to signalling server");
                PeerInfo peerInfo = ReceivePeerInfo();
                StartPeerConnection(peerInfo);
            }
            catch (Exception ex)
            {
                LOGGER.Error("connect exception", ex);
                started = false;
                ShutdownSocket();
            }
        }

        private Udt.Socket CreateSocket()
        {
            Udt.Socket socket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
            socket.ReuseAddress = true;
            socket.Bind(IPAddress.Any, localPort);
            return socket;

        }

        private void StartPeerConnection(PeerInfo peerInfo)
        {
            LOGGER.Info("Created new socket for peer connection");
            socket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
            socket.ReuseAddress = true;
            socket.SetSocketOption(Udt.SocketOptionName.Rendezvous, true);
            socket.Bind(IPAddress.Any, localPort);
            LOGGER.Info("Waiting for peer connection");
            socket.Connect(peerInfo.Address, peerInfo.Port);

            LOGGER.Info("peer connected, starting sender and listener threads");

            Thread receiveThread = new Thread(() => ListenMessages(socket));
            receiveThread.Start();

            Thread sendThread = new Thread(() => SendDate(socket));
            sendThread.Start();
            LOGGER.Info("connection done");
        }

        public PeerInfo ReceivePeerInfo()
        {
            PeerInfo peerInfo = null;
            using (Udt.NetworkStream st = new Udt.NetworkStream(socket, false))
            {
                LOGGER.Info("Waiting peer connection info from signalling server");
                byte[] peerInfoBytes = new byte[8];
                st.Read(peerInfoBytes, 0, peerInfoBytes.Length);

                LOGGER.Info("parsing peer connection info");
                byte[] peerAddressBytes = new byte[4];
                byte[] peerPortBytes = new byte[4];
                Array.Copy(peerInfoBytes, 0, peerAddressBytes, 0, peerAddressBytes.Length);
                Array.Copy(peerInfoBytes, 4, peerPortBytes, 0, peerPortBytes.Length);

                peerInfo = new PeerInfo();
                peerInfo.Address = new IPAddress(peerAddressBytes);
                peerInfo.Port = BitConverter.ToInt32(peerPortBytes, 0);
                LOGGER.Info("peer address: " + peerInfo.Address.ToString() + "  port: " + peerInfo.Port);
            }
            return peerInfo;
        }


        public void Disconnect()
        {
            if (started)
            {
                ShutdownSocket();
            }
            else
            {
                LOGGER.Error("not started");
                throw new Exception("not started");
            }
        }

        private void ShutdownSocket()
        {
            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch (Exception ex)
                {
                    LOGGER.Error("socket close exception", ex);
                }
            }
        }

        private void SendDate(Udt.Socket socket)
        {
            try
            {
                using (Udt.NetworkStream st = new Udt.NetworkStream(socket))
                using (BinaryWriter writer = new BinaryWriter(st))
                {
                    Thread.Sleep(10000);
                    while (started)
                    {
                        LOGGER.Info("Sending date");
                        writer.Write(DateTime.Now.ToLongTimeString());
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("send date exception", ex);
            }
            LOGGER.Info("send date stopped");
        }

        private void ListenMessages(Udt.Socket socket)
        {
            try
            {
                using (Udt.NetworkStream st = new Udt.NetworkStream(socket))
                using (BinaryReader reader = new BinaryReader(st))
                {
                    while (started)
                    {
                        LOGGER.Info("waiting for new message");
                        LOGGER.Info("RECEIVED: " + reader.ReadString());
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("message listen exception", ex);
            }
            LOGGER.Info("listen messages stopped");
        }
    }
}
