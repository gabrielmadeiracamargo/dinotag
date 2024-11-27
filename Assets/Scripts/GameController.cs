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
using TMPro;

public class GameController : MonoBehaviourPunCallbacks
{
    public static GameController Instance { get; private set; }

    [Header("Player related")]
    public GameObject player, stevenPlayer, dinoPlayer, canvas;
    public Transform[] spawnPoints;

    [Header("UI related")]
    [SerializeField] GameObject waitingText, settingsMenu;
    public ProgressBarCircle healthBar;
    public Image portrait;

    [Header("Game related")]
    public bool cutsceneEnded;
    public PlayableDirector _director;
    [SerializeField] GameObject startCutsceneObject;
    public GameObject playerWinCutscene, trexWinCutscene;
    public Material skybox;
    public float timer;


    // Start is called before the first frame update
    void Awake()
    {
        if (LevelManager.Instance != null) LevelManager.Instance._loaderCanvas.SetActive(false);

        stevenPlayer = GameObject.FindGameObjectWithTag("Player");
        skybox.SetFloat("_CubemapTransition", 1);
        skybox.SetFloat("_FogIntensity", 1);

        /*if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else */ Instance = this;

        //DontDestroyOnLoad(gameObject);
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        PlayLocalCutscene(); // Iniciar cutscene local
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        float transitionTime = 120f; // Dura��o total da transi��o em segundos
        float transitionValue = 1f - (timer / transitionTime); // Inverte de 1 para 0

        if (transitionValue > 0 && GameObject.Find("GameController").GetComponent<Timer>().enabled) // Verifica se ainda n�o chegou no m�nimo
        {
            GameObject.Find("Directional Light").GetComponent<Light>().intensity = 1.125f-transitionValue;
            skybox.SetFloat("_CubemapTransition", transitionValue);
            skybox.SetFloat("_FogIntensity", transitionValue);
        }


        waitingText.GetComponent<TextMeshProUGUI>().text = $"{PhotonNetwork.PlayerList.Length}/3 jogadores";
        /*else if (PhotonNetwork.PlayerList.Length == 2)
        {   
            if (!cutsceneEnded)
            {
                GameObject.FindGameObjectWithTag("Player").transform.position = new Vector3 (150,-150,0);
                GameObject.FindGameObjectWithTag("TRex").transform.position = new Vector3 (150,-150,0);
                startCutsceneObjects.SetActive(true);
            }
            if (waitingText.activeSelf) waitingText.SetActive(false);
        }*/


        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            settingsMenu.SetActive(!settingsMenu.activeInHierarchy);
        }

        if (settingsMenu.activeInHierarchy && Input.GetKeyDown(KeyCode.X))
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("Menu");
        }

        if (!cutsceneEnded && _director.time >= 17)
        {
            EndCutscene(); // Finaliza a cutscene automaticamente
        }

        if (Input.GetKeyDown(KeyCode.Space) && !cutsceneEnded)
        {
            SkipCutscene(); // Permitir pular com a tecla Espaço
        }

        /*if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!isOnTab) OnTabOpened();
            else OnTabClosed();
        }*/

        if (settingsMenu.activeInHierarchy)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            //ShowObjects(uiObjectsToHide, false);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            //ShowObjects(uiObjectsToHide, true);
        }
    }

    private void InstantiatePlayer()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        if (playerIndex == 0 || playerIndex == 2)
        {
            PhotonNetwork.Instantiate("Player", spawnPoints[playerIndex].position, Quaternion.identity);
            player = GameObject.FindGameObjectWithTag("Player");
            stevenPlayer = player;
        }
        else if (playerIndex == 1)
        {
            PhotonNetwork.Instantiate("TRex", spawnPoints[playerIndex].position, Quaternion.identity);
            player = dinoPlayer;
        }

        GameObject.FindGameObjectWithTag("Player").transform.position = spawnPoints[playerIndex].position;
        GameObject.FindGameObjectWithTag("TRex").transform.position = spawnPoints[playerIndex].position;
    }

    private void PlayLocalCutscene()
    {
        GameObject.Find("Canvas").SetActive(false);
        print($"existe: {GameObject.Find("Canvas")} está ativo: {GameObject.Find("Canvas").activeInHierarchy}");
        startCutsceneObject.SetActive(true);
        _director.Play();
        player.GetComponent<Player>().canMove = false;
    }

    public void SkipCutscene()
    {
        _director.time = 17; // Avançar até o final
        EndCutscene();
    }

    private void EndCutscene()
    {
        canvas.SetActive(true);
        cutsceneEnded = true;
        startCutsceneObject.SetActive(false);
        InstantiatePlayer();
        player.GetComponent<Player>().canMove = true;
        player.GetComponent<Player>().life = 100;
    }

    public void ShowObjects(GameObject[] objects, bool isShown)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(isShown);
        }
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("Menu");
    }
}
