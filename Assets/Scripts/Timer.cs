using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class Timer : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI timerText;
    public float timerDuration = 60*5f; // Duração do temporizador em segundos

    private bool timerOn = false;
    private double startTime;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameController.Instance.skipCutsceneButton.SetActive(true);
            startTime = PhotonNetwork.Time;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "StartTime", startTime } });
        }
        else
        {
            GameController.Instance.skipCutsceneButton.SetActive(false);
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
            {
                startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["StartTime"];
            }
        }

        timerOn = true;
    }

    void Update()
    {
        if (timerOn)
        {
            timerText.gameObject.SetActive(true);

            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
            {
                startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["StartTime"];
                double elapsedTime = PhotonNetwork.Time - startTime;
                float timeLeft = (float)(timerDuration - elapsedTime);

                if (timeLeft > 0)
                {
                    UpdateTimer(timeLeft);
                }
                else
                {
                    Debug.Log("Acabou o tempo!!");
                    timerOn = false;
                    UpdateTimer(0); // Mostra 00:00 no timer
                }
            }
        }
    }

    void UpdateTimer(float currentTime)
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
