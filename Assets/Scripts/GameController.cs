using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Playables;
using UnityEngine.SocialPlatforms.Impl;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviourPunCallbacks
{
    public static GameController Instance { get; private set; }

    [Header("Player related")]
    public GameObject player, stevenPlayer, dinoPlayer;
    public Transform[] spawnPoints;

    [Header("UI related")]
    [SerializeField] GameObject tabMenu, waitingText;
    [SerializeField] GameObject uiObjectsToHide, settingsMenu;
    public ProgressBarCircle healthBar;
    [SerializeField] GameObject skipCutsceneButton;
    public Image portrait;
    bool isOnTab;

    [Header("Game related")]
    public bool cutsceneEnded;
    [SerializeField] PlayableDirector _director;
    [SerializeField] GameObject cutsceneObjects;
    public Material skybox;
    public float timer;


    // Start is called before the first frame update
    void Awake()
    {
        if (LevelManager.Instance != null) LevelManager.Instance._loaderCanvas.SetActive(false);

        stevenPlayer = GameObject.FindGameObjectWithTag("Player");
        skybox.SetFloat("_CubemapTransition", 0);
        skybox.SetFloat("_FogIntensity", 0);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (PhotonNetwork.LocalPlayer.ActorNumber - 1 == 0) player = stevenPlayer;
        else if (PhotonNetwork.LocalPlayer.ActorNumber - 1 == 1) player = dinoPlayer;

        PhotonNetwork.Instantiate(player.name, spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].position, player.transform.rotation);
    }

    public void OnTabOpened()
    {
        isOnTab = true;
        tabMenu.SetActive(true);
    }
    public void OnTabClosed()
    {
        isOnTab = false;
        tabMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if ((int)skybox.GetFloat("_CubemapTransition") != 1)
        {
            skybox.SetFloat("_CubemapTransition", timer / (60));
            skybox.SetFloat("_FogIntensity", timer / (60));
        }

        if (PhotonNetwork.PlayerList.Length == 1) waitingText.SetActive(true);
        else if (PhotonNetwork.PlayerList.Length == 2)
        {   
            if (!cutsceneEnded)
            {
                GameObject.FindGameObjectWithTag("Player").transform.position = new Vector3 (150,-150,0);
                GameObject.FindGameObjectWithTag("TRex").transform.position = new Vector3 (150,-150,0);
                cutsceneObjects.SetActive(true);
                skipCutsceneButton.SetActive(true);
            }
            if (waitingText.activeSelf) waitingText.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!isOnTab) OnTabOpened();
            else OnTabClosed();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) settingsMenu.SetActive(!settingsMenu.activeInHierarchy);

        if (_director.time >= 62 && !cutsceneEnded)
        {
            GameObject.FindGameObjectWithTag("Player").transform.position = spawnPoints[0].position;
            GameObject.FindGameObjectWithTag("TRex").transform.position = spawnPoints[1].position;
            cutsceneEnded = true;
        }

        if (cutsceneEnded)
        {
            cutsceneObjects.SetActive(false);
            skipCutsceneButton.SetActive(false);
        }

        if (isOnTab)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            uiObjectsToHide.SetActive(false);
        }
        else
        {
            //Cursor.visible = false;
            //Cursor.lockState = CursorLockMode.Locked;
            uiObjectsToHide.SetActive(true);
        }
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    public void SkipCutscene()
    {
        skipCutsceneButton.SetActive(false);
        _director.time = 58.5f;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        SceneManager.LoadScene("Menu");
    }

}
