using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int client, Packet packet)
    {
        var clientIdCheck = packet.ReadInt();
        var username = packet.ReadString();

        Debug.Log($"{Server.Clients[client].Tcp.Socket.Client.RemoteEndPoint} connected");

        if (client != clientIdCheck)
        {
            Debug.Log($"Player \"{username}\" (ID: {client}) has assumed the wrong client ID ({clientIdCheck})");
        }

        Server.Clients[client].SendIntoGame(username);
    }

    public static void PlayerMovement(int client, Packet packet)
    {
        bool[] inputs = new bool[packet.ReadInt()];

        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = packet.ReadBool();
        }

        var rotation = packet.ReadQuaternion();
        var tick = packet.ReadLong();

        Server.Clients[client].Player.SetInput(inputs, rotation, tick);
    }
}
