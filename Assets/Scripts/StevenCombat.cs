using System.Collections;
using UnityEngine;
using Photon.Pun;

public class StevenCombat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    public float damage = 8f;
    [SerializeField] GameObject sword;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();

        // Inicializa o Collider da Sword
        if (GameObject.FindGameObjectWithTag("Sword") != null)
        {
            sword = GameObject.FindGameObjectWithTag("Sword");
            sword.GetComponent<BoxCollider>().enabled = false; // Desativa inicialmente
        }
    }

    void Update()
    {
        if ((!phView.IsMine) || (GetComponent<Gun>().hasGun == true && GetComponent<Gun>().hasSword == false)) return;

        // Gerenciamento de combo de espada
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
        }

        // Zera o combo se o tempo m�ximo for ultrapassado
        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        // Detec��o de clique para ataques
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }

        // Ativa��o do Collider da Sword
        if (noOfClicks == 0)
        {
            if (sword != null) sword.GetComponent<BoxCollider>().enabled = false;
        }
        else
        {
            if (sword != null) sword.GetComponent<BoxCollider>().enabled = true;
        }
    }

    void OnClick()
    {
        if (!phView.IsMine) return;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bite"))
        {
            phView.RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 3.5f);
            TRexCombat dinoCombat = other.GetComponentInParent<TRexCombat>();
            if (!dinoCombat.isBiting)
            {
                dinoCombat.isBiting = true;
                photonView.RPC("RPC_BeBitten", RpcTarget.All, dinoCombat.bite.transform.rotation);

                StartCoroutine(dinoCombat.ReleasePlayerAfterBite());
            }
        }
    }

    [PunRPC]
    public void RPC_BeBitten(Quaternion biteRotation)
    {
        StartCoroutine(BitePlayer(biteRotation));
    }

    private IEnumerator BitePlayer(Quaternion biteRotation)
    {
        GetComponent<Player>().canMove = false;
        float elapsedTime = 0f;

        while (elapsedTime < 1.0f) // Dura��o da mordida
        {
            transform.position = GameObject.FindGameObjectWithTag("Bite").transform.position;   // Posiciona o jogador na boca do dinossauro
            transform.rotation = biteRotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Vector3 releasePosition = GameObject.FindGameObjectWithTag("Bite").transform.position - (Vector3.up * 1.5f); // Ajusta para cair abaixo
        transform.position = releasePosition;
        GetComponent<Player>().canMove = true;
    }
}
