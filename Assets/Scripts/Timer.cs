using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Drawing;
using System;

public class Timer : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI timerText;
    public float timerDuration = 60 * 2f; // Duração do temporizador em segundos

    private bool timerOn = false;
    private double startTime;

    void Start()
    {
        timerText.gameObject.SetActive(false);
        if (PhotonNetwork.IsMasterClient)
        {
            startTime = PhotonNetwork.Time;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "StartTime", startTime } });
        }
        else
        {
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
                    UpdateTimer(0); // Mostra 0 segundos
                }
            }
        }
    }

    void UpdateTimer(float currentTime)
    {
        int seconds = Mathf.CeilToInt(currentTime); // Arredonda para cima para evitar "tempo negativo"
        timerText.text = $"{seconds}";
    }
}
