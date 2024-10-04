using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Combat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f; // Tempo de cooldown entre as mordidas
    private float nextBiteTime = 0f; // Guarda o tempo quando o dinossauro pode morder novamente
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage;
    [SerializeField] float knockbackForce = 250f;
    Vector3 direction;
    [SerializeField] private Transform bitePosition; // Defina no Inspector
    [SerializeField] private bool isBiting = false;
    [SerializeField] float biteDuration = 1.0f; // Duração da mordida
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
        if (GameObject.FindGameObjectWithTag("Bite") != null) bite = GameObject.FindGameObjectWithTag("Bite");
        if (bitePosition == null) bitePosition = GameObject.FindGameObjectWithTag("Bite").transform;

        if (GetComponent<Gun>() != null && !GetComponent<Gun>().hasSword) return;
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

        // Remove the nextFireTime and replace it with nextBiteTime
        if (Time.time > nextBiteTime) // Checking if the bite cooldown has ended
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
        isBiting = true;
        float elapsedTime = 0f; // Tempo que se passou desde o início da mordida

        // Enquanto o tempo da mordida não acabar
        while (elapsedTime < biteDuration)
        {
            // Atualizar a posição e rotação do jogador para ficar na boca
            SetPlayerPositionAndRotation();

            // Incrementar o tempo passado
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Liberar o jogador depois da mordida
        ReleasePlayer();

        // Adicionar delay entre as carregadas
        yield return new WaitForSeconds(cooldownTime);
        isBiting = false;
    }

    public void SetPlayerPositionAndRotation()
    {
        transform.position = bitePosition.position;
        transform.rotation = bitePosition.rotation;
    }

    public void ReleasePlayer()
    {
        // Define a posição do jogador logo abaixo da boca do dinossauro para simular a queda
        Vector3 releasePosition = bitePosition.position - (bitePosition.up * 1.5f); // Ajusta a altura para cair abaixo da boca
        transform.position = releasePosition;

        // Permite que o jogador recupere controle (se necessário)
        GetComponent<Player>().enabled = true;
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
                    GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, -10);
                    Destroy(other.gameObject);
                    break;
            }
        }
        else if (gameObject.CompareTag("Player") && other.CompareTag("Bite") && other.GetComponentInParent<PhotonView>().IsMine)
        {
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 6f);

            Combat dinoCombat = other.GetComponentInParent<Combat>();
            if (!dinoCombat.isBiting && Time.time >= nextBiteTime) // Verificar o cooldown antes de carregar o jogador
            {
                dinoCombat.isBiting = true;
                nextBiteTime = Time.time + cooldownTime; // Atualiza o tempo da próxima mordida

                PhotonView playerPhotonView = GetComponent<PhotonView>();
                if (playerPhotonView != null)
                {
                    playerPhotonView.RPC("RPC_BeBitten", RpcTarget.All, dinoCombat.bitePosition.position, dinoCombat.bitePosition.rotation);
                }

                StartCoroutine(ReleasePlayerAfterBite(dinoCombat));
            }
        }
    }

    private IEnumerator ReleasePlayerAfterBite(Combat dinoCombat)
    {
        yield return new WaitForSeconds(biteDuration); // Duração da mordida
        dinoCombat.isBiting = false; // Liberar o jogador

        if (gameObject.CompareTag("Player"))
        {
            ReleasePlayer(); // Soltar o player após a mordida
        }
    }
}
