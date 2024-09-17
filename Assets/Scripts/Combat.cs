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

    void Start()
    {
        if (!GetComponent<PhotonView>().IsMine) return;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (GetComponentInParent<Gun>().hasGun) return;

        bitePosition = GameObject.FindGameObjectWithTag("Bite").transform;

        if (!GetComponent<PhotonView>().IsMine) return;

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
            GameObject.FindGameObjectWithTag("Sword").GetComponent<BoxCollider>().enabled = false;
            GameObject.FindGameObjectWithTag("Bite").GetComponent<SphereCollider>().enabled = false;
        }
        else
        {
            GameObject.FindGameObjectWithTag("Sword").GetComponent<BoxCollider>().enabled = true;
            GameObject.FindGameObjectWithTag("Bite").GetComponent<SphereCollider>().enabled = true;
        }

        if (GetComponent<Player>().life <= 0.5f)
        {
            if (!GetComponent<PhotonView>().IsMine) return;
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

        // Removido: Atualização contínua da posição
    }

    void OnClick()
    {
        if (GetComponentInParent<Gun>().hasGun) return;

        if (!GetComponent<PhotonView>().IsMine) return;

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
        float biteDuration = 1.0f; // Duração da mordida em segundos
        float elapsedTime = 0f; // Tempo que se passou desde o início da mordida

        // Desabilitar controles do jogador enquanto está sendo mordido (se necessário)
        // GetComponent<Player>().enabled = false;

        // Enquanto o tempo da mordida não acabar
        while (elapsedTime < biteDuration)
        {
            // Atualizar a posição e rotação do jogador
            SetPlayerPositionAndRotation();

            // Incrementar o tempo passado
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Após a duração da mordida
        // Reabilitar controles do jogador (se necessário)
        // GetComponent<Player>().enabled = true;

        // Finaliza a coroutine após a duração da mordida
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

            // Somente o T-Rex controla a variável `isBiting`
            Combat dinoCombat = other.GetComponentInParent<Combat>();
            if (!dinoCombat.isBiting)
            {
                dinoCombat.isBiting = true;

                // Envia o RPC para o jogador mordido atualizar sua posição
                PhotonView playerPhotonView = GetComponent<PhotonView>();
                if (playerPhotonView != null)
                {
                    playerPhotonView.RPC("RPC_BeBitten", RpcTarget.All, dinoCombat.bitePosition.position, dinoCombat.bitePosition.rotation);
                }

                // Inicia a coroutine para liberar o jogador após a mordida
                StartCoroutine(ReleasePlayerAfterBite(dinoCombat));
            }
        }
    }

    private IEnumerator ReleasePlayerAfterBite(Combat dinoCombat)
    {
        yield return new WaitForSeconds(biteDuration); // Duração da mordida
        if (gameObject.CompareTag("Player"))
        {
            transform.rotation = new Quaternion(0,0,0,0);
        }
        dinoCombat.isBiting = false; // Só o T-Rex altera `isBiting`
    }

}