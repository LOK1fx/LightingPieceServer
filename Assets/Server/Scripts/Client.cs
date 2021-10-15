using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int DataBufferSize = 4069; //4069

    public int Id;
    public Player Player;

    public TCP Tcp;
    public UDP Udp;

    public Client(int id)
    {
        Id = id;

        Tcp = new TCP(Id);
        Udp = new UDP(Id);
    }

    public class TCP
    {
        public TcpClient Socket;

        private readonly int _id;

        private NetworkStream _stream;
        private Packet _receivedData;
        private byte[] _receiveBuffer;

        public TCP(int id)
        {
            _id = id;
        }

        public void Connect(TcpClient socket)
        {
            Socket = socket;
            Socket.ReceiveBufferSize = DataBufferSize;
            Socket.SendBufferSize = DataBufferSize;

            _stream = Socket.GetStream();

            _receivedData = new Packet();
            _receiveBuffer = new byte[DataBufferSize];
            _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

            ServerSend.Welcome(_id, "Welcome to the game", Server.CurrentTick);
        }

        public void Disconnect()
        {
            Socket.Close();
            _stream = null;
            _receivedData = null;
            _receiveBuffer = null;
            Socket = null;
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (Socket != null)
                {
                    _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception exception)
            {
                Debug.Log($"Error: sending data to player ID:{_id}. {exception}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var byteLength = _stream.EndRead(result);

                if (byteLength <= 0)
                {
                    Server.Clients[_id].Disconnect();

                    return;
                }

                var data = new byte[byteLength];

                Array.Copy(_receiveBuffer, data, byteLength);

                _receivedData.Reset(HandleData(data));
                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch (Exception exception)
            {
                Debug.Log($"Error! Receiving TCP Data Error: {exception}");

                Server.Clients[_id].Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            var packetLength = 0;

            _receivedData.SetBytes(data);

            if (_receivedData.UnreadLength() >= 4)
            {
                packetLength = _receivedData.ReadInt();

                if (packetLength <= 0)
                {
                    return true;
                }
            }

            while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
            {
                var packetBytes = _receivedData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        var packetId = packet.ReadInt();

                        Server.PacketHandlers[packetId](_id, packet);
                    }
                });

                packetLength = 0;
                if (_receivedData.UnreadLength() >= 4)
                {
                    packetLength = _receivedData.ReadInt();

                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 1)
            {
                return true;
            }

            return false;
        }
    }

    public class UDP
    {
        public IPEndPoint EndPoint;

        private int _id;

        public UDP(int id)
        {
            _id = id;
        }

        public void Connect(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public void Disconnect()
        {
            EndPoint = null;
        }

        public void SendData(Packet packet)
        {
            Server.SendUDPData(EndPoint, packet);
        }

        public void HandleData(Packet packetData)
        {
            var packetLength = packetData.ReadInt();
            var packetBytes = packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    Server.PacketHandlers[_packetId](_id, _packet);
                }
            });
        }
    }

    public void SendIntoGame(string playerName)
    {
        Player = NetworkManager.Instance.InstantiatePlayer();

        Player.Initialize(Id, playerName);

        foreach (Client client in Server.Clients.Values)
        {
            if (client.Player != null)
            {
                if (client.Id != Id)
                {
                    ServerSend.SpawnPlayer(Id, client.Player);
                }
            }
        }

        foreach (Client client in Server.Clients.Values)
        {
            if (client.Player != null)
            {
                ServerSend.SpawnPlayer(client.Id, Player);
            }
        }
    }

    public void Disconnect()
    {
        Debug.Log($"Player {Player.Username}:[{Player.Id}] has disconnected");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(Player.gameObject);

            Player = null;
        });  

        Tcp.Disconnect();
        Udp.Disconnect();
    }
}