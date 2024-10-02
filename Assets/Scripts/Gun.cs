using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Globalization;
using TMPro;

public class Gun : MonoBehaviour
{
    public int damage;
    public bool hasSword, hasGun;
    public int ammo = 7;
    RaycastHit hit;
    Transform camT;
    PhotonView phView;
    [SerializeField] public GameObject sword, gun;
    [SerializeField] float shootingAnimationDelay;
    [SerializeField] GameObject ammoText;

    void Start()
    {
        ammoText.gameObject.SetActive(true);
        camT = Camera.main.transform;
        phView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (hasGun)
        {
            AimGun(); // Handles aiming and shooting while aiming
        }

        if (Physics.Raycast(camT.position, camT.forward, out hit))
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (hit.collider.name == "SwordIcon") ChooseWeapon(true);
                else if (hit.collider.name == "GunIcon") ChooseWeapon(false);
            }
        }
    }

    void AimGun()
    {
        ammoText.SetActive(true);
        // Toggle aim mode on right mouse button
        if (Input.GetMouseButtonDown(1))
        {
            GetComponent<Animator>().SetBool("aiming", true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            GetComponent<Animator>().SetBool("aiming", false);
        }

        // Allow shooting while aiming
        if (GetComponent<Animator>().GetBool("aiming"))
        {
            if (Input.GetMouseButtonDown(0) && ammo > 0) // Left mouse button to shoot
            {
                ShootGun();
            }
        }
    }

    void ShootGun()
    {
        if (Physics.Raycast(camT.position, camT.forward, out hit))
        {
            if (hit.collider.gameObject.CompareTag("TRex"))
            {
                phView.RPC("RPC_SleepDino", RpcTarget.All, hit.collider.gameObject);
                hit.collider.gameObject.GetComponent<Combat>().phView.RPC("RPC_TakeDamage", RpcTarget.All, 5);
            }
                ammo--;
                ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
        }
        StartCoroutine(PlayShootingAnimation());
    }

    IEnumerator PlayShootingAnimation()
    {
        gun.GetComponentInChildren<ParticleSystem>().Play();
        GetComponent<Animator>().SetBool("aiming", false);
        GetComponent<Animator>().SetBool("shooted", true);
        yield return new WaitForSeconds(shootingAnimationDelay);
        GetComponent<Animator>().SetBool("shooted", false);
    }

    [PunRPC]
    public void RPC_SleepDino(GameObject dino)
    {
        StartCoroutine(WaitTillWakeUp(3, dino));
    }
    IEnumerator WaitTillWakeUp(float delay, GameObject dino)
    {
        dino.GetComponent<Animator>().SetBool("sleeping", true);
        dino.GetComponent<Player>().enabled = false;
        yield return new WaitForSeconds(delay);
        GetComponent<Animator>().SetBool("shooted", false);
        dino.GetComponent<Player>().enabled = true;
    }

    void ChooseWeapon(bool isSword)
    {
        sword.SetActive(isSword);
        gun.SetActive(!isSword);
        hasSword = isSword;
        hasGun = !isSword;
        GameObject.FindGameObjectsWithTag("Icons")[0].SetActive(false);
        GameObject.FindGameObjectsWithTag("Icons")[1].SetActive(false);
    }

    public void PickupAmmo(int amount)
    {
        ammo += amount;
        if (ammo > 7) ammo = 7;
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
    }
}
