using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviourPunCallbacks // usando pra outras coisas além da movimentação
{
    public GameObject playerCam;
    public CharacterController controller;

    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    public Animator _anim;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;

    public bool isGrounded;

    public TMP_Text nickTxt;
    public PhotonView phView;

    public Material skin2;

    [PunRPC]
    public void RPC_SetNickname(int index)
    {
        nickTxt.text = PhotonNetwork.PlayerList[index-1].NickName;
    }

    public void Awake()
    {
        phView = GetComponent<PhotonView>();

        if (!phView.IsMine) playerCam.SetActive(false);
        else nickTxt.gameObject.SetActive(false);


        phView.RPC("RPC_SetNickname", RpcTarget.OthersBuffered, phView.ControllerActorNr);
    }

    void Update()
    {
        if (!phView.IsMine) return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            photonView.RPC("RPC_ChangeColor", RpcTarget.AllBuffered);
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (x != 0 || z != 0) _anim.SetInteger("Speed", 1);
        else _anim.SetInteger("Speed", 0);

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

    }

    [PunRPC]
    public void RPC_ChangeColor()
    {
        for (int i = 0; i < GetComponentsInChildren<SkinnedMeshRenderer>().Length; i++)
        {
            GetComponentsInChildren<SkinnedMeshRenderer>()[i].material = skin2;
        }
    }
}