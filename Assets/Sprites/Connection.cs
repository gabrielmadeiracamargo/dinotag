using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Connection : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        print("Conectou ao servidor");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        print("Entrou no lobby");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnCreatedRoom()
    {
        print("Criou a sala");
    }

    public override void OnJoinedRoom()
    {
        print("Entrou na sala");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) // criar sala se não tiver nenhuma
    {
        print("Falhou na aleatória");
        PhotonNetwork.CreateRoom($"Room{Random.Range(100, 1000)} {Random.Range(100,1000)}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        print("Falhou na específica");
    }
}
