using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class Timer : MonoBehaviourPunCallbacks
{
    public float timeLeft;
    public bool timerOn;

    public TextMeshProUGUI timerText;

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.PlayerList.Length > 1)
        {
            timerText.gameObject.SetActive(true);
            timerOn = true;
        }

        if (timerOn)
        {
            if (timeLeft > 0)
            {
                timeLeft-= Time.deltaTime;
                UpdateTimer(timeLeft);
            }
            else
            {
                Debug.Log("acabou o tempo!!");
                timeLeft = 0;
                timerOn = false;
            }
        }
    }


    public void UpdateTimer(float currentTime)
    {
        currentTime += 1;

        float minutes = Mathf.Floor(currentTime / 60);
        float seconds = Mathf.Floor(currentTime % 60);

        timerText.text = string.Format("{0:00} : {1:00}", minutes, seconds);
    }
}
