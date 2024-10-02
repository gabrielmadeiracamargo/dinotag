using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;
using System.Threading.Tasks;

public class Connection : MonoBehaviourPunCallbacks
{
    public static GameObject connection;
    public GameObject connectionStatus;
    public string input;
    public string nick;
    public int maxPlayers = 2;
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
        /*roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = System.Convert.ToByte(1);*/

        PhotonNetwork.ConnectUsingSettings();

        PhotonNetwork.LocalPlayer.NickName = nick;

        roomOptions.CustomRoomProperties = roomHashtable;
        roomOptions.MaxPlayers = System.Convert.ToByte(maxPlayers);

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

    public void ReadNickname(string s)
    {
        nick = s;
        PhotonNetwork.LocalPlayer.NickName = nick;
    }

    public void CreateRoom()
    {
        roomOptions.MaxPlayers = System.Convert.ToByte(maxPlayers);
        PhotonNetwork.CreateRoom(input, roomOptions, null);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(input);
    }

    public void QuickPlay()
    {
        PhotonNetwork.JoinRandomOrCreateRoom(roomHashtable, System.Convert.ToByte(maxPlayers));
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
    
    public override async void OnJoinedRoom()
    {
        print(roomOptions.MaxPlayers - PhotonNetwork.PlayerList.Length);
        //print($"Entrou na sala {input}");

        if (PhotonNetwork.CurrentRoom.PlayerCount > roomOptions.MaxPlayers)
        {
            PhotonNetwork.LeaveRoom(); // Sai da sala se o número de jogadores exceder o limite
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            await Task.Delay(100);
            PhotonNetwork.LoadLevel("cene");
        }
        else
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
        //print($"Entrou na sala {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) // criar sala se não tiver nenhuma
    {
        int randomRoomNumber = Random.Range(0, 99999);
        //print("Falhou na aleatória");
        PhotonNetwork.CreateRoom($"Room {randomRoomNumber}", roomOptions, null);
        PhotonNetwork.JoinRoom($"Room {randomRoomNumber}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        //print("Falhou na específica");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
