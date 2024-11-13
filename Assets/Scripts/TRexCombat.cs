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
    public bool isBiting = false;
    public float biteDuration = 1.0f; // Dura��o da mordida
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

        // Gerenciamento de anima��es de combo de mordida
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

        // Detec��o de ataque de mordida
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }

        // Ativa��o do Collider de mordida
        if (noOfClicks == 0)
        {
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = false;
        }
        else
        {
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = true;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Sword"))
        {
            // L�gica de dano ao TRex quando � atingido por uma espada
            if (other.GetComponentInParent<PhotonView>().IsMine)
            {
                float damageToApply = other.GetComponentInParent<StevenCombat>().damage;
                GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, damageToApply);
            }
        }
        else if (other.CompareTag("Food"))
        {
            // Regenera vida ao T-Rex ao consumir comida
            GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, -10f);
            Destroy(other.gameObject);
        }
    }

    [PunRPC]
    public void RPC_BeBitten(Vector3 bitePos, Quaternion biteRotation)
    {
        StartCoroutine(BitePlayer(bitePos, biteRotation));
    }

    private IEnumerator BitePlayer(Vector3 bitePos, Quaternion biteRotation)
    {
        isBiting = true;
        float elapsedTime = 0f;

        // Enquanto a dura��o da mordida n�o acabar, mant�m o jogador preso
        while (elapsedTime < biteDuration)
        {
            SetPlayerPositionAndRotation();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Libera o jogador depois da mordida
        photonView.RPC("RPC_ReleasePlayer", RpcTarget.All);

        yield return new WaitForSeconds(cooldownTime); // Tempo de cooldown entre as mordidas
        isBiting = false;
    }

    public void SetPlayerPositionAndRotation()
    {
        GetComponent<Player>().canMove = false;
        // Atualiza a posi��o e rota��o do jogador para coincidir com a boca do dinossauro
        transform.position = bite.transform.position;
        transform.rotation = bite.transform.rotation;
    }

    [PunRPC]
    public void RPC_ReleasePlayer()
    {
        GetComponent<Player>().canMove = true;
        // Libera o jogador logo abaixo da boca do dinossauro para simular a queda
        Vector3 releasePosition = bite.transform.position - (bite.transform.up * 1.5f); // Ajusta a altura para cair abaixo da boca
        transform.position = releasePosition;

        // Permite que o jogador recupere controle
    }

    public IEnumerator ReleasePlayerAfterBite(PhotonView playerPhotonView)
    {
        yield return new WaitForSeconds(biteDuration);
        isBiting = false;

        // Solta o jogador, movendo-o para uma posi��o abaixo da boca do T-Rex
        playerPhotonView.RPC("RPC_ReleasePlayer", RpcTarget.All, bite.transform.position - (bite.transform.up * 1.5f));
    }

    [PunRPC]
    public void RPC_SleepDino()
    {
        if (phView.IsMine)
        {
            GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 5f);
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
