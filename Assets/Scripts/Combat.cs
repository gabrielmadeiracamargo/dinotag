using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Combat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f;
    private float nextFireTime = 0f;
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage;
    [SerializeField] float knockbackForce = 250f;
    Vector3 direction;
    [SerializeField] private Transform bitePosition; // Defina no Inspector
    [SerializeField] private bool isBiting = false;
    [SerializeField] float biteDuration;
    GameObject sword, bite;
    public PhotonView phView;

    void Start()
    {
        phView = GetComponent<PhotonView>();
        if (!phView.IsMine) return;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (GetComponent<Gun>() != null && !GetComponent<Gun>().hasSword) return;
        if (GameObject.FindGameObjectWithTag("Sword") != null) sword = GameObject.FindGameObjectWithTag("Sword");
        if (GameObject.FindGameObjectWithTag("Bite") != null) bite = GameObject.FindGameObjectWithTag("Bite");

        if (bitePosition != null) bitePosition = bite.transform;

        if (!phView.IsMine) return;

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

        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }
        if (Time.time > nextFireTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnClick();
            }
        }
        if (noOfClicks == 0)
        {
            sword.GetComponent<BoxCollider>().enabled = false;
            bite.GetComponent<SphereCollider>().enabled = false;
        }
        else
        {
            sword.GetComponent<BoxCollider>().enabled = true;
            bite.GetComponent<SphereCollider>().enabled = true;
        }

        if (GetComponent<Player>().life <= 0.5f)
        {
            if (!phView.IsMine) return;
            switch (gameObject.tag)
            {
                case "Player":
                    SceneManager.LoadScene("DinoWin");
                    break;
                case "TRex":
                    SceneManager.LoadScene("PlayerWin");
                    break;
            }
        }

        // Removido: Atualiza��o cont�nua da posi��o
    }

    void OnClick()
    {
        if (GetComponent<Gun>() != null && !GetComponent<Gun>().hasSword) return;

        if (!phView.IsMine) return;

        lastClickedTime = Time.time;
        noOfClicks++;
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
    public void RPC_TakeDamage(float damage)
    {
        GetComponent<Player>().life -= damage;
    }

    [PunRPC]
    public void RPC_BeBitten(Vector3 bitePos, Quaternion biteRotation)
    {
        StartCoroutine(BitePlayer(bitePos, biteRotation));
    }

    private IEnumerator BitePlayer(Vector3 bitePos, Quaternion biteRotation)
    {
        float biteDuration = 1.0f; // Dura��o da mordida em segundos
        float elapsedTime = 0f; // Tempo que se passou desde o in�cio da mordida

        // Desabilitar controles do jogador enquanto est� sendo mordido (se necess�rio)
        // GetComponent<Player>().enabled = false;

        // Enquanto o tempo da mordida n�o acabar
        while (elapsedTime < biteDuration)
        {
            // Atualizar a posi��o e rota��o do jogador
            SetPlayerPositionAndRotation();

            // Incrementar o tempo passado
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ap�s a dura��o da mordida
        // Reabilitar controles do jogador (se necess�rio)
        // GetComponent<Player>().enabled = true;

        // Finaliza a coroutine ap�s a dura��o da mordida
    }

    public void SetPlayerPositionAndRotation()
    {
        transform.position = bitePosition.position;
        transform.rotation = bitePosition.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player's Sword hits T-Rex
        if (gameObject.CompareTag("TRex"))
        {
            // Aplica dano ao T-Rex
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
                    GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, -10);
                    Destroy(other.gameObject);
                    break;
            }
        }
        // T-Rex's Bite hits Player
        else if (gameObject.CompareTag("Player") && other.CompareTag("Bite") && other.GetComponentInParent<PhotonView>().IsMine)
        {
            // Aplica dano ao jogador
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 6f);

            // Somente o T-Rex controla a vari�vel `isBiting`
            Combat dinoCombat = other.GetComponentInParent<Combat>();
            if (!dinoCombat.isBiting)
            {
                dinoCombat.isBiting = true;

                // Envia o RPC para o jogador mordido atualizar sua posi��o
                PhotonView playerPhotonView = GetComponent<PhotonView>();
                if (playerPhotonView != null)
                {
                    playerPhotonView.RPC("RPC_BeBitten", RpcTarget.All, dinoCombat.bitePosition.position, dinoCombat.bitePosition.rotation);
                }

                // Inicia a coroutine para liberar o jogador ap�s a mordida
                StartCoroutine(ReleasePlayerAfterBite(dinoCombat));
            }
        }
    }

    private IEnumerator ReleasePlayerAfterBite(Combat dinoCombat)
    {
        yield return new WaitForSeconds(biteDuration); // Dura��o da mordida
        if (gameObject.CompareTag("Player"))
        {
            transform.rotation = new Quaternion(0,0,0,0);
        }
        dinoCombat.isBiting = false; // S� o T-Rex altera `isBiting`
    }

}