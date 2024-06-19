using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    public static GameController Instance { get; private set; }

    public GameObject player, stevenPlayer, dinoPlayer;
    public Transform[] spawnPoints;
    [SerializeField] GameObject pauseMenu, waitingText;
    bool isOnPause;

    // Start is called before the first frame update
    void Awake()
    {
        /*if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);*/
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (PhotonNetwork.LocalPlayer.ActorNumber - 1 == 0) player = stevenPlayer;
        else if (PhotonNetwork.LocalPlayer.ActorNumber - 1 == 1) player = dinoPlayer;

        PhotonNetwork.Instantiate(player.name, spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].position, player.transform.rotation);
    }

    public void OnPauseOpened()
    {
        isOnPause = true;
        pauseMenu.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void OnPauseClosed()
    {
        isOnPause = false;
        pauseMenu.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.PlayerList.Length == 1) 
            waitingText.SetActive(true);
        else if (PhotonNetwork.PlayerList.Length == 2 && waitingText.activeSelf) waitingText.SetActive(false);

        if (Input.GetButtonDown("Cancel"))
        {
            if (!isOnPause) OnPauseOpened();
            else OnPauseClosed();
        }
    }
}
