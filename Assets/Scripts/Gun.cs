using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class Gun : MonoBehaviourPunCallbacks
{
    public int damage;
    public bool hasSword, hasGun;
    public int ammo = 14;
    public int maxAmmo = 14;
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
        ammoText = GameObject.Find("Ammo Count");
        ammoText.SetActive(false);
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        ammo = maxAmmo;
        camT = Camera.main.transform;


        if (PhotonNetwork.PlayerList.Length >= 3)
        {
            AssignWeaponsBasedOnPlayers(); // Sincroniza estado inicial das armas
        }
        else
        {
            EnableWeaponSwitching(); // Permite troca livre de armas para 2 jogadores
        }
    }


    void Update()
    {
        print(PhotonNetwork.PlayerList.Length);
        if (!phView.IsMine) return;


        // Apenas os dois primeiros jogadores podem alternar entre as armas
        if (PhotonNetwork.PlayerList.Length == 2)
        {
            HandleWeaponSwitching();
        }
        else AssignWeaponsBasedOnPlayers();

        // Atualizar HUD de munição e mecânica de tiro
        if (hasGun)
        {
            AimGun();
            print("esse tem a arma");
            ammoText.SetActive(true);
        }
        else
        {
            ammoText.SetActive(false);
        }
    }

    [PunRPC]
    public void RPC_AssignInitialWeapons(int playerActorNumber, bool isSword)
    {
        if (phView.Owner.ActorNumber == playerActorNumber)
        {
            hasSword = isSword;
            hasGun = !isSword;
            EquipInitialWeapon(); // Configura a arma inicial
        }
    }

    private void AssignWeaponsBasedOnPlayers()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;

        if (PhotonNetwork.PlayerList.Length >= 3)
        {
            if (playerIndex == 1)
            {
                phView.RPC("RPC_ChooseWeapon", RpcTarget.All, true); // Equip Sword
            }
            else
            {
                phView.RPC("RPC_ChooseWeapon", RpcTarget.All, false); // Equip Gun
            }
        }
        else
        {
            EnableWeaponSwitching();
        }
    }

    private void HandleWeaponSwitching()
    {
        // Troca de arma com base no scroll do mouse
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            weaponIndex = (weaponIndex + 1) % totalWeapons; // Scroll up
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, weaponIndex == 0); // 0 = Sword
            GetComponent<GameTips>().UpdateTips();
        }
        else if (scroll < 0f) 
        {
            weaponIndex = (weaponIndex - 1 + totalWeapons) % totalWeapons; // Scroll down
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, weaponIndex == 0); // 0 = Sword
            GetComponent<GameTips>().UpdateTips();
        }
    }

    private void EquipSword()
    {
        hasSword = true;
        hasGun = false;

        sword.SetActive(true);
        gun.SetActive(false);

        Debug.Log("Sword equipped.");
        UpdateWeaponSprite();
    }

    private void EquipGun()
    {
        hasSword = false;
        hasGun = true;

        sword.SetActive(false);
        gun.SetActive(true);

        Debug.Log("Gun equipped.");
        UpdateWeaponSprite();
    }

    private void EnableWeaponSwitching()
    {
        Debug.Log("Weapon switching enabled for this player.");
        EquipInitialWeapon(); // Configurar arma inicial com base no estado atual
    }

    private void EquipInitialWeapon()
    {
        if (hasSword)
        {
            EquipSword();
        }
        else if (hasGun)
        {
            EquipGun();
        }
    }

    void AimGun()
    {
        if (!phView.IsMine) return;
        
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/{maxAmmo}";

        if (Input.GetMouseButtonDown(1))
        {
            print("esse ta mirando");
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
                print("esse ta atirando");
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
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/{maxAmmo}";
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
        if (isSword)
        {
            hasSword = true;
            hasGun = false;
            sword.SetActive(true);
            gun.SetActive(false);
        }
        else
        {
            hasSword = false;
            hasGun = true;
            sword.SetActive(false);
            gun.SetActive(true);
        }

        UpdateWeaponSprite();
    }

    void UpdateWeaponSprite()
    {
        if (weaponSprite != null)
        {
            weaponSprite.SetActive(true);
            weaponSprite.GetComponent<UnityEngine.UI.Image>().sprite = hasSword ? swordSprite : gunSprite;
        }
    }

    public void PickupAmmo(int amount)
    {
        ammo += amount;
        if (ammo > maxAmmo) ammo = maxAmmo;
    }
}
