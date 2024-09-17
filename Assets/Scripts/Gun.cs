using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Globalization;

public class Gun : MonoBehaviour
{
    public int damage;
    public bool hasGun;
    RaycastHit hit;
    Transform camT;
    PhotonView phView;
    [SerializeField] GameObject sword, gun;

    void Start()
    {
        camT = Camera.main.transform;
        phView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!phView.IsMine) return;
        if (Physics.Raycast(camT.position, camT.forward, out hit)) 
        {
            if (hit.collider.CompareTag("Player"))
            {
                hit.collider.GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.All, damage);
            }

            if (Input.GetMouseButtonUp(0))
            {
                switch (hit.collider.gameObject.name)
                {
                    case "SwordIcon":
                        ChooseWeapon(true);
                        break;
                    case "GunIcon":
                        ChooseWeapon(false);
                        hasGun = true;
                        break;
                }
            }
        }
    }

    void ChooseWeapon(bool isSword)
    {
        sword.SetActive(isSword);
        gun.SetActive(!isSword);
        GameObject.FindGameObjectsWithTag("Icons")[0].SetActive(false);
        GameObject.FindGameObjectsWithTag("Icons")[1].SetActive(false);
    }
}
