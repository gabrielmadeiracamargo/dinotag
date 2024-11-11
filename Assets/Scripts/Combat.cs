using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Combat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f; // Tempo de cooldown entre as mordidas
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage = 5f;
    [SerializeField] float knockbackForce = 250f;
    Vector3 direction;
    [SerializeField] private Transform bitePosition; // Defina no Inspector
    [SerializeField] private bool isBiting = false;
    [SerializeField] float biteDuration = 1.0f; // Duração da mordida
    [SerializeField] GameObject sword, bite;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();

        // Inicializando Sword e Bite com os respectivos Colliders
        if (GameObject.FindGameObjectWithTag("Bite") != null)
        {
            bite = GameObject.FindGameObjectWithTag("Bite");
            bite.GetComponent<SphereCollider>().enabled = false; // Desativa inicialmente
        }

        if (GameObject.FindGameObjectWithTag("Sword") != null)
        {
            sword = GameObject.FindGameObjectWithTag("Sword");
            sword.GetComponent<BoxCollider>().enabled = false; // Desativa inicialmente
        }
    }

    void Update()
    {
        if (bitePosition == null && bite != null)
        {
            bitePosition = bite.transform;
        }

        if (!phView.IsMine) return;

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

        // Zera os cliques se o tempo máximo de combo passar
        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        // Detecção de clique para ataques
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }

        // Ativação e Desativação dos Colliders da Espada e Mordida
        if (noOfClicks == 0)
        {
            if (sword != null) sword.GetComponent<BoxCollider>().enabled = false;
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = false;
        }
        else
        {
            if (sword != null) sword.GetComponent<BoxCollider>().enabled = true;
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = true;
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

    [PunRPC]
    public void RPC_SleepDino()
    {
        if (phView.IsMine)
        {
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 5f);
            StartCoroutine(Sleep());
        }
    }

    private IEnumerator Sleep()
    {
        GetComponent<Player>().canMove = false;
        GetComponent<Animator>().SetBool("sleeping", true);
        yield return new WaitForSeconds(3); // Dorme por 3 segundos
        GetComponent<Player>().canMove = true;
        GetComponent<Animator>().SetBool("sleeping", false);
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

        // Enquanto a duração da mordida não acabar, mantém o jogador preso
        while (elapsedTime < biteDuration)
        {
            SetPlayerPositionAndRotation();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Libera o jogador depois da mordida
        ReleasePlayer();

        yield return new WaitForSeconds(cooldownTime); // Tempo de cooldown entre as mordidas
        isBiting = false;
    }

    public void SetPlayerPositionAndRotation()
    {
        GetComponent<Player>().canMove = false;
        // Atualiza a posição e rotação do jogador para coincidir com a boca do dinossauro
        transform.position = bitePosition.position;
        transform.rotation = bitePosition.rotation;
    }

    public void ReleasePlayer()
    {
        GetComponent<Player>().canMove = true;
        // Libera o jogador logo abaixo da boca do dinossauro para simular a queda
        Vector3 releasePosition = bitePosition.position - (bitePosition.up * 1.5f); // Ajusta a altura para cair abaixo da boca
        transform.position = releasePosition;

        // Permite que o jogador recupere controle
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameObject.CompareTag("TRex"))
        {
            switch (other.gameObject.tag)
            {
                case "Sword":
                    if (other.GetComponentInParent<PhotonView>().IsMine)
                    {
                        float damageToApply = other.GetComponentInParent<Combat>().damage;
                        GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, damageToApply);
                    }
                    break;
                case "Food":
                    GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, -10f); // Regenera vida
                    Destroy(other.gameObject);
                    break;
            }
        }
        else if (gameObject.CompareTag("Player") && other.CompareTag("Bite") && other.GetComponentInParent<PhotonView>().IsMine)
        {
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 6f);

            Combat dinoCombat = other.GetComponentInParent<Combat>();
            if (!dinoCombat.isBiting)
            {
                dinoCombat.isBiting = true;

                PhotonView playerPhotonView = GetComponent<PhotonView>();
                if (playerPhotonView != null)
                {
                    playerPhotonView.RPC("RPC_BeBitten", RpcTarget.All, bitePosition.position, bitePosition.rotation);
                }

                StartCoroutine(ReleasePlayerAfterBite(dinoCombat));
            }
        }
    }

    private IEnumerator ReleasePlayerAfterBite(Combat dinoCombat)
    {
        yield return new WaitForSeconds(biteDuration);
        dinoCombat.isBiting = false;

        if (gameObject.CompareTag("Player"))
        {
            ReleasePlayer();
        }
    }
}
