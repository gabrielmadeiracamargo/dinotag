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
        // Detecta se a tecla 1 (Espada) ou 2 (Arma) foi pressionada
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, true); // Escolhe a espada
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            phView.RPC("RPC_ChooseWeapon", RpcTarget.All, false); // Escolhe a arma
        }

        if (hasGun)
        {
            ammoText.gameObject.SetActive(true);
            AimGun();
        }
        else
        {
            ammoText.gameObject.SetActive(false);
        }
    }

    void AimGun()
    {
        ammoText.SetActive(true);
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
        if (Physics.Raycast(camT.position, camT.forward, out hit))
        {
            if (hit.collider.gameObject.GetComponentInParent<Player>().gameObject.CompareTag("TRex"))
            {
                print(hit.collider.gameObject.GetComponentInParent<Player>().gameObject);
                PhotonView dinoView = hit.collider.gameObject.GetComponent<PhotonView>();

                // Envia o RPC para todos os clientes
                if (dinoView != null)
                {
                    //hit.collider.gameObject.GetComponentInParent<Player>().life -= 5;
                    dinoView.RPC("RPC_SleepDino", RpcTarget.All); // T-Rex dorme (apenas no cliente dele)
                }
            }
            else print(hit.collider.name);
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
    void RPC_ChooseWeapon(bool isSword)
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<Gun>().sword.SetActive(isSword);
        GameObject.FindGameObjectWithTag("Player").GetComponent<Gun>().gun.SetActive(!isSword);
        GameObject.FindGameObjectWithTag("Player").GetComponent<Gun>().hasSword = isSword;
        GameObject.FindGameObjectWithTag("Player").GetComponent<Gun>().hasGun = !isSword;
    }

    public void PickupAmmo(int amount)
    {
        ammo += amount;
        if (ammo > 7) ammo = 7;
        ammoText.GetComponent<TextMeshProUGUI>().text = $"{ammo}/7";
    }
}
