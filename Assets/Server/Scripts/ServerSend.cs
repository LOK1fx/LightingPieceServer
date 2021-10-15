using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    #region Tcp

    private static void SendTCPData(int targetClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[targetClient].Tcp.SendData(packet);
    }

    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();

        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            Server.Clients[i].Tcp.SendData(packet);
        }
    }

    private static void SendTCPDataToAll(int exceptionClient, Packet packet)
    {
        packet.WriteLength();

        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            if (i != exceptionClient)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
    }

    #endregion

    #region Udp

    private static void SendUDPData(int targetClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[targetClient].Udp.SendData(packet);
    }

    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();

        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            Server.Clients[i].Udp.SendData(packet);
        }
    }

    private static void SendUDPDataToAll(int exceptionClient, Packet packet)
    {
        packet.WriteLength();

        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            if (i != exceptionClient)
            {
                Server.Clients[i].Udp.SendData(packet);
            }
        }
    }

    #endregion

    #region Packets

    public static void Welcome(int client, string message, long tick)
    {
        using (Packet packet = new Packet((int)ServerPackets.Welcome))
        {
            packet.Write(message);
            packet.Write(client);
            packet.Write(tick);

            SendTCPData(client, packet);
        }
    }

    public static void SpawnPlayer(int targetClient, Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.SpawnPlayer))
        {
            packet.Write(player.Id);
            packet.Write(player.Username);
            packet.Write(player.transform.position);
            packet.Write(player.transform.rotation);

            SendTCPData(targetClient, packet);
        }
    }

    public static void PlayerPosition(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.PlayerPosition))
        {
            packet.Write(player.Id);
            packet.Write(player.transform.position);

            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerRotation(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.PlayerRotation))
        {
            packet.Write(player.Id);
            packet.Write(player.transform.rotation);

            SendUDPDataToAll(player.Id, packet);
        }
    }

    #endregion
}
