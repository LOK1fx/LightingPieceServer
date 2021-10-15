using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static long CurrentTick;

    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();

    public delegate void PacketHandler(int client, Packet packet);
    public static Dictionary<int, PacketHandler> PacketHandlers;

    private static TcpListener _tcpListener;
    private static UdpClient _udpListener;

    public static void Start(int maxPlayers, int port)
    {
        MaxPlayers = maxPlayers;
        Port = port;

        Debug.Log("Starting server...");

        InitializeServerData();

        _tcpListener = new TcpListener(IPAddress.Any, Port);
        _tcpListener.Start();
        _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnecrCallback), null);

        _udpListener = new UdpClient(Port);
        _udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started. Port: {Port}");
    }

    private static void InitializePacketHandlers()
    {
        PacketHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ClientPackets.WelcomeReceived, ServerHandle.WelcomeReceived },
            { (int)ClientPackets.PlayerMovement, ServerHandle.PlayerMovement }
        };
    }

    public static void Stop()
    {
        _tcpListener.Stop();
        _udpListener.Close();
    }

    private static void InitializeServerData()
    {
        for (int i = 0; i <= MaxPlayers; i++)
        {
            Clients.Add(i, new Client(i));
        }

        InitializePacketHandlers();
    }

    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
    {
        try
        {
            if (clientEndPoint != null)
            {
                _udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error sending data to {clientEndPoint} via UDP: {_ex}");
        }
    }

    private static void TCPConnecrCallback(IAsyncResult result)
    {
        var client = _tcpListener.EndAcceptTcpClient(result);

        _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnecrCallback), null);

        Console.Beep();
        Debug.Log($"Client connection from {client.Client.RemoteEndPoint}");

        for (int i = 0; i <= MaxPlayers; i++)
        {
            if (Clients[i].Tcp.Socket == null)
            {
                Clients[i].Tcp.Connect(client);

                return;
            }
        }

        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect: Sever fall :(");
    }

    private static void UDPReceiveCallback(IAsyncResult result)
    {
        try
        {
            var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var data = _udpListener.EndReceive(result, ref clientEndPoint);
            _udpListener.BeginReceive(UDPReceiveCallback, null);

            if (data.Length < 4)
            {
                return;
            }

            using (Packet packet = new Packet(data))
            {
                int clientId = packet.ReadInt();

                if (clientId == 0)
                {
                    return;
                }

                if (Clients[clientId].Udp.EndPoint == null)
                {
                    Clients[clientId].Udp.Connect(clientEndPoint);
                    return;
                }

                if (Clients[clientId].Udp.EndPoint.ToString() == clientEndPoint.ToString())
                {
                    Clients[clientId].Udp.HandleData(packet);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.Log($"Error receiving UDP data: {exception}");
        }
    }
}
