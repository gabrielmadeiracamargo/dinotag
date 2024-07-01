using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class Player : MonoBehaviourPunCallbacks // usando pra outras coisas além da movimentação
{
    public float life;

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

    public List<Material> skins = new List<Material>(); // Lista de skins
    private int currentSkinIndex = 0; // Índice da skin atual

    public void Awake()
    {
        phView = GetComponent<PhotonView>();

        if (!phView.IsMine) playerCam.SetActive(false);
        else nickTxt.gameObject.SetActive(false);

        phView.RPC("RPC_SetNickname", RpcTarget.OthersBuffered, phView.ControllerActorNr);
    }

    void Update()
    {
        //if (PhotonNetwork.IsConnected && GameController.Instance.startedGame && PhotonNetwork.PlayerList.Length == 2) PhotonNetwork.LoadLevel("Menu");
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (PhotonNetwork.IsConnected && !phView.IsMine) return;


        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            photonView.RPC("RPC_ChangeSkin", RpcTarget.AllBuffered);
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (x != 0 || z != 0) _anim.SetInteger("Speed", 1);
        else _anim.SetInteger("Speed", 0);

        print($"x: {x} z: {z}");

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

    }

    public void ChangeSkin()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PhotonView>().RPC("RPC_ChangeSkin", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_ChangeSkin()
    {
        // Alterna para a próxima skin na lista
        currentSkinIndex = (currentSkinIndex + 1) % skins.Count;
        Material newSkin = skins[currentSkinIndex];

        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.material = newSkin;
        }
    }

    [PunRPC]
    public void RPC_TakeDamage(int damage)
    {
        life -= damage;
        //if (life <= 0); gameObject.SetActive(false);
    }

    [PunRPC]
    public void RPC_SetNickname(int index)
    {
        nickTxt.text = PhotonNetwork.PlayerList[index - 1].NickName;
    }
}
