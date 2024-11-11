using System.Collections;
using UnityEngine;
using Photon.Pun;

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

        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }

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

    [PunRPC]
    public void RPC_BeBitten(Vector3 bitePos, Quaternion biteRotation)
    {
        StartCoroutine(BitePlayer(bitePos, biteRotation));
    }

    private IEnumerator BitePlayer(Vector3 bitePos, Quaternion biteRotation)
    {
        float elapsedTime = 0f;
        GetComponent<Player>().canMove = false;

        while (elapsedTime < 1.0f) // Durando o tempo da mordida
        {
            transform.position = bitePos;
            transform.rotation = biteRotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    [PunRPC]
    public void RPC_ReleasePlayer(Vector3 releasePosition)
    {
        transform.position = releasePosition;
        GetComponent<Player>().canMove = true;
    }
}
