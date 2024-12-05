using System.Collections;
using UnityEngine;
using Photon.Pun;

public class TRexCombat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f; // Tempo de cooldown entre as mordidas
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage = 5f;
    public bool isBiting, isBeingAttacked = false, isCooldown = false; // Adicionado isCooldown
    public float biteDuration = 1.0f; // Duração da mordida
    public GameObject bite;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();

        // Inicializa o Collider de Bite
        if (GameObject.FindGameObjectWithTag("Bite") != null)
        {
            bite = GameObject.FindGameObjectWithTag("Bite");
            bite.GetComponent<SphereCollider>().enabled = false; // Desativa inicialmente
        }
    }

    void Update()
    {
        if (!phView.IsMine) return;

        if (GameObject.Find("Ammo Count") != null) GameObject.Find("Ammo Count").SetActive(false);

        // Gerenciamento de animações de combo de mordida
        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && anim.GetCurrentAnimatorStateInfo(0).IsName("hit1"))
        {
            anim.SetBool("hit1", false);
        }
        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && anim.GetCurrentAnimatorStateInfo(0).IsName("hit2"))
        {
            anim.SetBool("hit2", false);
        }
        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && anim.GetCurrentAnimatorStateInfo(0).IsName("hit3"))
        {
            anim.SetBool("hit3", false);
            noOfClicks = 0;

            // Inicia cooldown após o final do hit3
            if (!isCooldown)
            {
                StartCoroutine(StartCooldown());
            }
        }

        // Zera o combo se o tempo máximo for ultrapassado
        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        // Detecção de ataque de mordida
        if (Input.GetMouseButtonDown(0) && !isCooldown)
        {
            OnClick();
        }

        // Sincronização do estado do Collider de mordida entre os clientes
        if (noOfClicks == 0)
        {
            if (bite != null && bite.GetComponent<SphereCollider>().enabled)
            {
                phView.RPC("RPC_SetBiteCollider", RpcTarget.AllBuffered, false);
            }
        }
        else
        {
            if (bite != null && !bite.GetComponent<SphereCollider>().enabled)
            {
                phView.RPC("RPC_SetBiteCollider", RpcTarget.AllBuffered, true);
            }
        }
    }

    void OnClick()
    {
        if (!phView.IsMine || isBiting) return;

        noOfClicks++;
        lastClickedTime = Time.time;

        if (noOfClicks == 1)
        {
            damage = Random.Range(4f, 7f);
            anim.SetBool("hit1", true);
        }

        noOfClicks = Mathf.Clamp(noOfClicks, 0, 3);

        if (noOfClicks >= 2 && anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && anim.GetCurrentAnimatorStateInfo(0).IsName("hit1"))
        {
            damage = Random.Range(7f, 10f);
            anim.SetBool("hit1", false);
            anim.SetBool("hit2", true);
        }
        if (noOfClicks >= 3 && anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && anim.GetCurrentAnimatorStateInfo(0).IsName("hit2"))
        {
            damage = 7f;
            anim.SetBool("hit2", false);
            anim.SetBool("hit3", true);
        }
    }

    private IEnumerator StartCooldown()
    {
        isCooldown = true; // Ativa cooldown
        yield return new WaitForSeconds(cooldownTime); // Aguarda o tempo de cooldown
        isCooldown = false; // Desativa cooldown
    }

    private void OnTriggerEnter(Collider other)
    {
        print($"ele é o {other.gameObject.name}");

        if (other.CompareTag("Sword") && GetComponentInParent<PhotonView>().IsMine && isBeingAttacked == false && GameObject.Find("GameController").GetComponent<Timer>().seconds < 119)
        {
            other.gameObject.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>().Play();
            isBeingAttacked = true;
            GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 12f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        print($"ele é o {other.gameObject.name}");

        if (other.CompareTag("Sword") && GetComponentInParent<PhotonView>().IsMine && isBeingAttacked == true)
        {
            isBeingAttacked = false;
        }
    }

    [PunRPC]
    public void RPC_ReleasePlayer(Vector3 releasePosition)
    {
        if (!phView.IsMine) return;

        if (bite != null)
        {
            bite.GetComponent<SphereCollider>().enabled = false;
        }

        // Libera o jogador para a posição especificada
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = releasePosition;
            player.GetComponent<Player>().canMove = true;
        }
    }

    public IEnumerator ReleasePlayerAfterBite()
    {
        yield return new WaitForSeconds(biteDuration);
        isBiting = false;

        // Solta o jogador, movendo-o para uma posição abaixo da boca do T-Rex
        phView.RPC("RPC_ReleasePlayer", RpcTarget.All, bite.transform.position - (bite.transform.up * 1.5f));
    }

    [PunRPC]
    public void RPC_SetBiteCollider(bool isEnabled)
    {
        if (bite != null)
        {
            bite.GetComponent<SphereCollider>().enabled = isEnabled;
        }
    }

    [PunRPC]
    public void RPC_SleepDino()
    {
        if (phView.IsMine)
        {
            GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 2.5f);
            StartCoroutine(Sleep());
        }
    }

    private IEnumerator Sleep()
    {
        GetComponent<Player>().canMove = false;
        anim.SetBool("sleeping", true);
        yield return new WaitForSeconds(3); // Dorme por 3 segundos
        GetComponent<Player>().canMove = true;
        anim.SetBool("sleeping", false);
    }
}
