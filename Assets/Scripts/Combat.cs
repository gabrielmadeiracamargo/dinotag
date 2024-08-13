using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Combat : MonoBehaviourPunCallbacks
{
    private Animator anim;
    public float cooldownTime = 2f;
    private float nextFireTime = 0f;
    public static int noOfClicks = 0;
    float lastClickedTime = 0;
    float maxComboDelay = 1;
    float damage;

    void Start()
    {
        if (!GetComponent<PhotonView>().IsMine) return;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
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

        if (noOfClicks == 0) GameObject.FindGameObjectWithTag("Sword").GetComponent<BoxCollider>().enabled = false;
        else GameObject.FindGameObjectWithTag("Sword").GetComponent<BoxCollider>().enabled = true;
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
        if (GetComponent<PhotonView>().IsMine) GetComponent<Player>().life -= damage;
        //if (life <= 0); gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.CompareTag("Sword") && gameObject.CompareTag("TRex"))  || (other.gameObject.CompareTag("Bite") && gameObject.CompareTag("Player"))) GetComponent<PhotonView>().RPC("RPC_TakeDamage", RpcTarget.All, damage);

    }
}
