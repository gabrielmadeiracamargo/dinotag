using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Gun : MonoBehaviour
{
    public int damage;
    public bool hasGun;
    RaycastHit hit;
    Transform camT;
    PhotonView phView;

    void Start()
    {
        camT = Camera.main.transform;
        phView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!phView.IsMine) return;

        if (Input.GetButtonDown("Fire1") && hasGun)
        {
            if (Physics.Raycast(camT.position, camT.forward, out hit)) 
            {
                if (hit.collider.CompareTag("Player"))
                {
                    hit.collider.GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.All, damage);
                }
            }
        }
    }
}
