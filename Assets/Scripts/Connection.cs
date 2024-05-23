using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class Connection : MonoBehaviourPunCallbacks
{
    public static GameObject connection;
    public GameObject connectionStatus;
    [SerializeField] private string input, nick;
    Hashtable roomHashtable = new Hashtable();
    RoomOptions roomOptions = new RoomOptions();

    // Start is called before the first frame update
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (connection != gameObject)
        {
            connection = gameObject;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }

        //roomHashtable.Add("Score", 0);
        roomOptions.CustomRoomProperties = roomHashtable;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 2;

        PhotonNetwork.ConnectUsingSettings();

        PhotonNetwork.LocalPlayer.NickName = nick;
    }

    private void Update()
    {
        if (connectionStatus!=null)
        {
            if (PhotonNetwork.IsConnected) connectionStatus.GetComponent<TextMeshProUGUI>().text = "    Status: <color=green>Online";
            else connectionStatus.GetComponent<TextMeshProUGUI>().text = "    Status: <color=red>Offline";
        }
    }

    public void ReadRoomInputName(string s)
    {
        input = s;
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(input, roomOptions);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(input);
    }

    public void QuickPlay()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
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
    }

    public override void OnCreatedRoom()
    {
        
        print($"Criou a sala {input}");
    }

    public override void OnJoinedRoom()
    {
        print($"Entrou na sala {input}");
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("Game");
        print($"Entrou na sala {PhotonNetwork.CurrentRoom.Name}");
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
