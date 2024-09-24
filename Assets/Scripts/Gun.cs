using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Globalization;

public class Gun : MonoBehaviour
{
    public int damage;
    public bool hasSword, hasGun;
    RaycastHit hit;
    Transform camT;
    PhotonView phView;
    [SerializeField] public GameObject sword, gun;

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
                if (hit.collider.name == "SwordIcon") ChooseWeapon(true);
                else if (hit.collider.name == "GunIcon") ChooseWeapon(false);
            }
        }
    }

    void ChooseWeapon(bool isSword)
    {
        sword.SetActive(isSword);
        gun.SetActive(!isSword);
        hasSword = isSword;
        hasGun = !isSword;
        GameObject.FindGameObjectsWithTag("Icons")[0].SetActive(false);
        GameObject.FindGameObjectsWithTag("Icons")[1].SetActive(false);
        print(hasGun);
    }
}
