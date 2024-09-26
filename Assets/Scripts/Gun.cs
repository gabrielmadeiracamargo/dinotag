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
        if (hasGun) AimGun();
    }

    void AimGun()
    {
        if (Input.GetMouseButtonUp(1))
        {
            GetComponent<Animator>().SetBool("aiming", !GetComponent<Animator>().GetBool("aiming"));
        }

        if (GetComponent<Animator>().GetBool("aiming") == true)
        {
            if (Input.GetMouseButtonUp(0)) ShootGun();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (hit.collider.name == "SwordIcon") ChooseWeapon(true);
            else if (hit.collider.name == "GunIcon") ChooseWeapon(false);
        }
    }

    void ShootGun()
    {
        if (Physics.Raycast(camT.position, camT.forward, out hit))
        {
            if (hit.collider.gameObject.CompareTag("TRex"))
            {
                hit.collider.gameObject.GetComponent<Combat>().phView.RPC("RPC_TakeDamage", RpcTarget.All, 5);
            }
        }
        StartCoroutine(PlayShootingAnimation());
    }

    IEnumerator PlayShootingAnimation()
    {
        GetComponent<Animator>().SetBool("shooted", true);
        yield return new WaitForEndOfFrame();
        GetComponent<Animator>().SetBool("shooted", false);
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
