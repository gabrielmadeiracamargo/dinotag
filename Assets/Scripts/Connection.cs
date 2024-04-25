using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Connection : MonoBehaviourPunCallbacks
{
    public static GameObject connection;

    // Start is called before the first frame update
    void Awake()
    {
        if (connection != gameObject)
        {
            connection = gameObject;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        Hashtable roomHashtable = new Hashtable();
        roomHashtable.Add("Score", 0);
        RoomOptions roomOptions = new RoomOptions();

        roomOptions.CustomRoomProperties = roomHashtable;
        roomOptions.IsOpen= true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 1;
        PhotonNetwork.CreateRoom("Random" + Random.Range(0, 100), roomOptions);
    }

    // Funções do Photon
    public override void OnConnectedToMaster()
    {
        print("Conectou ao servidor");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        print("Entrou no lobby");
        SceneManager.LoadScene("Lobby");
        //PhotonNetwork.JoinRandomRoom();
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
