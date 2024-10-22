using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class TRexCombat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f; // Tempo de cooldown entre as mordidas
    [SerializeField] private Transform bitePosition;
    [SerializeField] private bool isBiting = false;
    [SerializeField] float biteDuration = 1.0f; // Dura��o da mordida
    [SerializeField] GameObject bite;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();

        // Inicializando Bite com o respectivo Collider
        if (GameObject.FindGameObjectWithTag("Bite") != null)
        {
            bite = GameObject.FindGameObjectWithTag("Bite");
            bite.GetComponent<SphereCollider>().enabled = false; // Desativa inicialmente
        }
    }

    void Update()
    {
        if (bite != null)
        {
            bitePosition = bite.transform;
        }

        if (!phView.IsMine) return;

        // Gerenciamento da l�gica de mordida (ativa��o do colisor)
        if (Input.GetMouseButtonDown(0))
        {
            OnBite();
        }

        // Ativa��o e Desativa��o do Collider de Mordida
        if (isBiting)
        {
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = true;
        }
        else
        {
            if (bite != null) bite.GetComponent<SphereCollider>().enabled = false;
        }
    }

    void OnBite()
    {
        if (!phView.IsMine || isBiting) return;

        isBiting = true;
        StartCoroutine(PerformBite());
    }

    private IEnumerator PerformBite()
    {
        // L�gica para executar a mordida
        anim.SetTrigger("bite");
        yield return new WaitForSeconds(biteDuration);
        isBiting = false;
    }

    // Fun��o chamada ao receber dano
    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (phView.IsMine)
        {
            gameObject.GetComponent<Player>().life -= damage;
            Debug.Log("T-Rex perdeu vida. Vida atual: " + gameObject.GetComponent<Player>().life);
        }
    }

    // Faz o T-Rex entrar no estado de "dormir"
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
}
