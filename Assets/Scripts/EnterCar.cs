using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EnterCar : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject stevenModel, car;
    [SerializeField] bool isOnCar;

    private void OnTriggerStay(Collider collider)
    {
        if (!GetComponent<PhotonView>().IsMine) return;

        if (collider.gameObject.name == "Car" && Input.GetMouseButton(0) && PhotonNetwork.PlayerList.Length == 2) GetComponent<PhotonView>().RPC("RPC_EnterCar", RpcTarget.AllBuffered);
    }
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "Fuga" && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("fugacarro");
        }
    }

    private void Update()
    {
        if (!GetComponent<PhotonView>().IsMine) return;

        if (isOnCar && Input.GetMouseButtonUp(1))
        {
            GetComponent<PhotonView>().RPC("RPC_ExitCar", RpcTarget.All);
        }

        if (isOnCar)
        {
            gameObject.transform.position = car.transform.position;
        }
    }

    [PunRPC]
    public void RPC_EnterCar()
    {
        if (!GetComponent<PhotonView>().IsMine) return;
        stevenModel.SetActive(false);
        car.GetComponent<CarController>().enabled = true;
        car.transform.Find("Camera").gameObject.SetActive(true);
        isOnCar = true;
    }

    [PunRPC]
    public void RPC_ExitCar()
    {
        if (!GetComponent<PhotonView>().IsMine) return;

        isOnCar = false;
        stevenModel.SetActive(true);
        car.GetComponent<CarController>().enabled = false;
        car.transform.Find("Camera").gameObject.SetActive(false);
    }
}
