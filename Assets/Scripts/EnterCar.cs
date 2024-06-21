using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EnterCar : MonoBehaviourPunCallbacks
{

    private void OnTriggerStay(Collider collider)
    {
        print("colidindo");
        if (collider.gameObject.CompareTag("Player") && Input.GetMouseButtonUp(0))
        {
            print("era pra entrar");
            GetComponent<PhotonView>().RPC("RPC_EnterCar", RpcTarget.AllBuffered);
        }
    }
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "Fuga" && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Pastas temporárias de cada um/Nery/fugacarro");
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
        print("rpc");
        GameController.Instance.stevenPlayer.SetActive(false);
        GetComponent<CarController>().enabled = true;
        GetComponentInChildren<Camera>().enabled = true;
    }

    [PunRPC]
    public void RPC_ExitCar()
    {
        GameController.Instance.stevenPlayer.SetActive(true);
        GameController.Instance.stevenPlayer.transform.position = transform.position;
        GetComponent<CarController>().enabled = false;
        GetComponentInChildren<Camera>().enabled = false;
    }
}
