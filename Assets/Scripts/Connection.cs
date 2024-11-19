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
    public int maxPlayers = 3;
    Hashtable roomHashtable = new Hashtable();
    RoomOptions roomOptions = new RoomOptions();

    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(WaitToConnect());
        PhotonNetwork.ConnectUsingSettings();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.LocalPlayer.NickName = nick;

        roomOptions.CustomRoomProperties = roomHashtable;
        roomOptions.MaxPlayers = System.Convert.ToByte(maxPlayers);
    }

    IEnumerator WaitToConnect()
    {
        yield return new WaitForSeconds(0.5f);
    }

    private void Update()
    {
        if (connectionStatus != null)
        {
            if (PhotonNetwork.IsConnected)
                connectionStatus.GetComponent<TextMeshProUGUI>().text = "    Status: <color=green>Online";
            else
                connectionStatus.GetComponent<TextMeshProUGUI>().text = "    Status: <color=red>Offline";
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
        // Verifica se está no lobby antes de tentar se conectar
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        PhotonNetwork.JoinRoom(input);
    }

    public void QuickPlay()
    {
        // Verifica se está no lobby antes de tentar se conectar
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

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
            //PhotonNetwork.CurrentRoom.IsOpen = false;
            //PhotonNetwork.CurrentRoom.IsVisible = false;
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message) // criar sala se não tiver nenhuma
    {
        int randomRoomNumber = Random.Range(0, 99999);
        PhotonNetwork.CreateRoom($"Room {randomRoomNumber}", roomOptions, null);
        PhotonNetwork.JoinRoom($"Room {randomRoomNumber}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        print("Falhou ao tentar entrar na sala específica");
    }

    // Método para sair do jogo e retornar ao menu
    public void OnQuitGame()
    {
        // Verifica se está em uma sala e sai dela
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // Carrega a cena do Menu
        SceneManager.LoadScene("Menu");
    }

    // Método para sair completamente da aplicação
    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    // Adiciona o evento de saída de sala para garantir que o jogador seja desconectado corretamente
    public override void OnLeftRoom()
    {
        print("Saiu da sala com sucesso");
    }
}
