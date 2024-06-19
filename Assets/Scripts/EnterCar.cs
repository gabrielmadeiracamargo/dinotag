using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EnterCar : MonoBehaviourPunCallbacks
{

    [SerializeField] GameObject player;

    private void OnTriggerStay(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player") && Input.GetMouseButtonUp(0))
        {
            player = GameObject.FindGameObjectWithTag("Player");
            GetComponent<PhotonView>().RPC("RPC_EnterCar", RpcTarget.AllBuffered);
        }
    }

    private void Update()
    {
        if (GetComponent<CarController>().enabled == true && Input.GetMouseButtonUp(1))
        {
            GetComponent<PhotonView>().RPC("RPC_ExitCar", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void RPC_EnterCar()
    {
        player.SetActive(false);
        GetComponent<CarController>().enabled = true;
        GetComponentInChildren<Camera>().enabled = true;
    }

    [PunRPC]
    public void RPC_ExitCar()
    {
        player.SetActive(true);
        player.transform.position = transform.position;
        GetComponent<CarController>().enabled = false;
        GetComponentInChildren<Camera>().enabled = false;
    }
}
