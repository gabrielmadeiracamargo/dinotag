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

    [Header("Weapon Sprites")]
    [SerializeField] GameObject weaponSprite; // Sprite UI for current weapon
    [SerializeField] Sprite swordSprite;      // Sprite for sword
    [SerializeField] Sprite gunSprite;        // Sprite for gun

    private int weaponIndex = 0; // 0 = Sword, 1 = Gun
    private const int totalWeapons = 2;

    void Start()
    {
        camT = Camera.main.transform;
        phView = GetComponent<PhotonView>();
        UpdateWeaponSprite(); // Initialize the weapon sprite
    }

    void Update()
    {
        if (!phView.IsMine) return;

        // Mouse scroll wheel input for changing weapons
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            weaponIndex = (weaponIndex + 1) % totalWeapons; // Scroll up
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, weaponIndex == 0); // 0 = Sword
        }
        else if (scroll < 0f)
        {
            weaponIndex = (weaponIndex - 1 + totalWeapons) % totalWeapons; // Scroll down
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, weaponIndex == 0); // 0 = Sword
        }

        // Only update ammo UI if gun is equipped
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

    void AimGun()
    {
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
        if (!phView.IsMine) return;

        RaycastHit[] hits = Physics.RaycastAll(camT.position, camT.forward);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.CompareTag("TRex"))
            {
                PhotonView dinoView = GameObject.Find("TRex(Clone)").GetComponent<PhotonView>();

                if (dinoView != null)
                {
                    dinoView.RPC("RPC_SleepDino", RpcTarget.All);
                }
            }
            else
            {
                print(hit.collider.name);
            }
        }

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
        gunScript.hasGun = !isSword;
        gunScript.UpdateWeaponSprite();
    }

    void UpdateWeaponSprite()
    {
        if (weaponSprite != null)
        {
            weaponSprite.GetComponent<UnityEngine.UI.Image>().sprite = hasSword ? swordSprite : gunSprite;
        }
    }

    public void PickupAmmo(int amount)
    {
        ammo += amount;
        if (ammo > 7) ammo = 7;
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
    }
}
