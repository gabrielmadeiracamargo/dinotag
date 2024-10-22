using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class StevenCombat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage = 5f;
    [SerializeField] GameObject sword;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();

        // Inicializando Sword com o respectivo Collider
        if (GameObject.FindGameObjectWithTag("Sword") != null)
        {
            sword = GameObject.FindGameObjectWithTag("Sword");
            sword.GetComponent<BoxCollider>().enabled = false; // Desativa inicialmente
        }
    }

    void Update()
    {
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

        // Ativação e Desativação do Collider da Espada
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

    // Função chamada pelo cliente humano ao acertar o tiro
    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (phView.IsMine)
        {
            gameObject.GetComponent<Player>().life -= damage;
            Debug.Log("Perdeu vida. Vida atual: " + gameObject.GetComponent<Player>().life);
        }
    }

    [PunRPC]
    public void RPC_BeBitten(Vector3 bitePos, Quaternion biteRotation)
    {
        StartCoroutine(BitePlayer(bitePos, biteRotation));
    }

    private IEnumerator BitePlayer(Vector3 bitePos, Quaternion biteRotation)
    {
        float biteDuration = 1.0f;
        GetComponent<Player>().canMove = false;

        // Enquanto a duração da mordida não acabar, mantém o jogador preso
        float elapsedTime = 0f;
        while (elapsedTime < biteDuration)
        {
            transform.position = bitePos;
            transform.rotation = biteRotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        GetComponent<Player>().canMove = true;
    }
}
