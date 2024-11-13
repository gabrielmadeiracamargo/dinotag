using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class Gun : MonoBehaviour
{
    public int damage;
    public bool hasSword, hasGun;
    public int ammo = 7;
    RaycastHit hit;
    [SerializeField] Transform camT;
    PhotonView phView;
    [SerializeField] public GameObject sword, gun;
    [SerializeField] float shootingAnimationDelay;
    [SerializeField] GameObject ammoText;

    void Start()
    {
        camT = Camera.main.transform;
        phView = GetComponent<PhotonView>();
    }

    void Update()
    {
        // Detect if the key 1 (Sword) or 2 (Gun) is pressed
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, true); // Choose the sword
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, false); // Choose the gun
        }

        // Only update ammo UI for the local player if they have the gun equipped
        if (phView.IsMine)
        {
            if (hasGun)
            {
                ammoText.SetActive(true);
                AimGun();
            }
            else
            {
                ammoText.SetActive(false);
            }
        }
    }

    void AimGun()
    {
        // Continue only if this is the local player's gun
        if (!phView.IsMine) return;

        if (Input.GetMouseButtonDown(1))
        {
            GetComponent<Animator>().SetBool("aiming", true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            GetComponent<Animator>().SetBool("aiming", false);
        }

        if (GetComponent<Animator>().GetBool("aiming"))
        {
            Debug.DrawRay(camT.position, camT.forward * 100f, Color.red, 2f);
            if (Input.GetMouseButtonDown(0) && ammo > 0)
            {
                ShootGun();
            }
        }
    }

    void ShootGun()
    {
        // Check if this instance is owned by the local player
        if (!phView.IsMine) return;

        RaycastHit[] hits = Physics.RaycastAll(camT.position, camT.forward);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.CompareTag("TRex"))
            {
                PhotonView dinoView = GameObject.Find("TRex(Clone)").GetComponent<PhotonView>();

                // Send the RPC to all clients for the T-Rex
                if (dinoView != null)
                {
                    dinoView.RPC("RPC_SleepDino", RpcTarget.All); // Make T-Rex sleep (client-specific)
                }
            }
            else
            {
                print(hit.collider.name);
            }
        }

        // Reduce ammo and update the UI for the local player only
        ammo--;
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
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
    void RPC_ChooseWeapon(bool isSword)
    {
        Gun gunScript = GameObject.FindGameObjectWithTag("Player").GetComponent<Gun>();
        gunScript.sword.SetActive(isSword);
        gunScript.gun.SetActive(!isSword);
        gunScript.hasSword = isSword;
        gunScript.GetComponent<Gun>().hasGun = !isSword;
    }

    public void PickupAmmo(int amount)
    {
        ammo += amount;
        if (ammo > 7) ammo = 7;
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
    }
}
