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

    void Start()
    {
        if (!GetComponent<PhotonView>().IsMine) return;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
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

        if (GetComponent<Player>().life <= 0.5)
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
    }

    void OnClick()
    {
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

    private void OnTriggerEnter(Collider other)
    {
        // Player's Sword hits T-Rex
        if (gameObject.CompareTag("TRex") && other.CompareTag("Sword") && other.GetComponentInParent<PhotonView>().IsMine)
        {
            // Apply damage to T-Rex
            float damageToApply = other.GetComponentInParent<Combat>().damage; // Get the damage value from the player
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, damageToApply);
        }
        // T-Rex's Bite hits Player
        else if (gameObject.CompareTag("Player") && other.CompareTag("Bite") && other.GetComponentInParent<PhotonView>().IsMine)
        {
            direction = (transform.position - other.transform.position).normalized;
            GetComponentInParent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.AllBuffered, 6f);
        }
    }
}
