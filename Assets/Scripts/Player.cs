using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main script for third-person movement of the character in the game.
/// Make sure that the object that will receive this script (the player) 
/// has the Player tag and the Character Controller component.
/// </summary>
public class Player : MonoBehaviourPunCallbacks
{

    [Tooltip("Speed ​​at which the character moves. It is not affected by gravity or jumping.")]
    public float velocity = 5f;
    [Tooltip("This value is added to the speed value while the character is sprinting.")]
    public float sprintAdittion = 3.5f;
    [Tooltip("The higher the value, the higher the character will jump.")]
    public float jumpForce = 18f;
    [Tooltip("Stay in the air. The higher the value, the longer the character floats before falling.")]
    public float jumpTime = 0.85f;
    [Space]
    [Tooltip("Force that pulls the player down. Changing this value causes all movement, jumping and falling to be changed as well.")]
    public float gravity = 9.8f;

    float jumpElapsedTime = 0;

    // Player states
    bool isJumping = false;
    bool isSprinting = false;

    // Inputs
    public bool canMove = true;
    float inputHorizontal;
    private int inputXHash = Animator.StringToHash("inputX");
    float inputVertical;
    private int inputYHash = Animator.StringToHash("inputY");
    bool inputJump;
    bool inputSprint;

    [SerializeField] Animator animator;
    [SerializeField] CharacterController cc;

    public float life;

    public TMP_Text nickTxt;
    public PhotonView phView;
    public GameObject playerCam;

    [SerializeField] GameObject minimapCamera;
    [SerializeField] Sprite portrait, dinoLostImage, dinoWinImage;

    private int currentSkinIndex = 0; // Índice da skin atual

    public RenderTexture minimapTexture;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    void Awake()
    {
        phView = GetComponent<PhotonView>();

        if (!phView.IsMine) playerCam.SetActive(false);
        else nickTxt.gameObject.SetActive(false);

        phView.RPC("RPC_SetNickname", RpcTarget.OthersBuffered, phView.ControllerActorNr);

        // Message informing the user that they forgot to add an animator
        if (animator == null)
            Debug.LogWarning("Hey buddy, you don't have the Animator component in your player. Without it, the animations won't work.");

        if (!phView.IsMine) minimapCamera.SetActive(false);
    }

    // Update is only being used here to identify keys and trigger animations
    void Update()
    {
        if (!photonView.IsMine) return;
        if (GameObject.FindGameObjectWithTag("Minimap") != null) 
        {
            print("colocando imagem do minimapa");
            GameObject.FindGameObjectWithTag("Minimap").GetComponent<RawImage>().texture = minimapTexture;
        }
        if (GameController.Instance.portrait.sprite != null) GameController.Instance.portrait.sprite = this.portrait;
        if (GameController.Instance.healthBar != null) GameController.Instance.healthBar.BarValue = life;

        if (GameObject.Find("GameController").GetComponent<Timer>().enabled == false && GameController.Instance.cutsceneEnded == true) 
        {
            GameObject.Find("GameController").GetComponent<Timer>().enabled = true;
        }
        // Input checkers
        if (canMove)
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
            inputJump = Input.GetAxis("Jump") == 1f;
            inputSprint = Input.GetAxis("Fire3") == 1f;
        }

        animator.SetFloat(inputXHash, inputHorizontal);
        animator.SetFloat(inputYHash, inputVertical);

        // walk and Sprint animation
        if (cc.isGrounded && animator != null)
        {
            float minimumSpeed = 0.9f;

            // Sprint
            isSprinting = cc.velocity.magnitude > minimumSpeed && inputSprint;
            animator.SetBool("sprint", isSprinting);
        }

        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            if (!isSprinting) animator.SetBool("walk", true);
            else animator.SetBool("sprint", true);
        }
        else animator.SetBool("walk", false);

        // Jump animation
        if (animator != null)
            animator.SetBool("air", !cc.isGrounded);

        // Handle can jump or not
        if (inputJump && cc.isGrounded)
        {
            print("vai pular");
            isJumping = true;
        }

        if (!isJumping && !cc.isGrounded)
        {
            animator.SetBool("fall", true);
        }

        HeadHittingDetect();
        GroundCheck();

        if (life <= 0) 
        {
            print($"{gameObject.tag} morreu");
            phView.RPC("RPC_EndGame", RpcTarget.All, gameObject.tag);
        }

        if (GameController.Instance.playerWinCutscene.GetComponent<PlayableDirector>().time >= 25f || GameController.Instance.trexWinCutscene.GetComponent<PlayableDirector>().time >= 8f) 
        {
            if (PhotonNetwork.InRoom)
            {
                StartCoroutine(DisconnectAndLoadMenu());
            }

            // Carrega a cena do Menu
            print("acabou");
            //SceneManager.LoadScene("Menu");
        }

        if (GameController.Instance.settingsMenu.activeInHierarchy && Input.GetKeyDown(KeyCode.X))
        {
            if (PhotonNetwork.InRoom)
            {
                StartCoroutine(DisconnectAndLoadMenu());
            }
        }
    }

    // With the inputs and animations defined, FixedUpdate is responsible for applying movements and actions to the player
    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // Sprinting velocity boost
        float velocityAdittion = 0;
        if (isSprinting)
            velocityAdittion = sprintAdittion;

        // Direction movement
        float directionX = inputHorizontal * (velocity + velocityAdittion) * Time.deltaTime;
        float directionZ = inputVertical * (velocity + velocityAdittion) * Time.deltaTime;
        float directionY = 0;

        // Jump handler
        if (isJumping)
        {
            // Apply inertia and smoothness when climbing the jump
            directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, jumpElapsedTime / jumpTime) * Time.deltaTime;

            // Jump timer
            jumpElapsedTime += Time.deltaTime;
            if (jumpElapsedTime >= jumpTime)
            {
                isJumping = false;
                jumpElapsedTime = 0;
            }
        }

        // Add gravity to Y axis
        directionY -= gravity * Time.deltaTime;

        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = (Camera.main.transform.forward * directionZ) + (Camera.main.transform.right * directionX);

        // Ensure player movement is aligned with the camera, ignoring the Y axis to avoid moving upward
        horizontalDirection.y = 0;

        Vector3 moviment = verticalDirection + horizontalDirection;
        cc.Move(moviment);
    }

    void GroundCheck()
    {
        bool isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    //This function makes the character end his jump if he hits his head on something
    void HeadHittingDetect()
    {
        if (!photonView.IsMine) return;

        float headHitDistance = 1.1f;
        Vector3 ccCenter = transform.TransformPoint(cc.center);
        float hitCalc = cc.height / 2f * headHitDistance;

        // Uncomment this line to see the Ray drawed in your characters head
        // Debug.DrawRay(ccCenter, Vector3.up * headHeight, Color.red);

        if (Physics.Raycast(ccCenter, Vector3.up, hitCalc))
        {
            jumpElapsedTime = 0;
            isJumping = false;
        }
    }

    [PunRPC]
    public void RPC_SetNickname(int index)
    {
        nickTxt.text = PhotonNetwork.PlayerList[index - 1].NickName;
        if (nickTxt.text == null || nickTxt.text == "")
        {
            if (nickTxt.gameObject.name.StartsWith("Steven Nick")) nickTxt.text = "Steven";
            else nickTxt.text = "T-Rex";
        }
        if (nickTxt.gameObject.name.StartsWith("Steven Nick")) nickTxt.text += " Spielberg";
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (GetComponent<PhotonView>().IsMine)
        {
            gameObject.GetComponent<Player>().life -= damage;
            Debug.Log("Perdeu vida. Vida atual: " + gameObject.GetComponent<Player>().life);
        }
    }

    [PunRPC]
    public void RPC_EndGame(string deadTag)
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        switch (deadTag)
        {
            case "Player":
                GameController.Instance.trexWinCutscene.SetActive(true); break;
            case "TRex":
                GameController.Instance.playerWinCutscene.SetActive(true); break;
        }
        GameObject.Find("Canvas").SetActive(false);
    }

    [PunRPC]
    public void RPC_DisableSceneSync()
    {
        print("Desativando sincronização automática de cenas.");
        PhotonNetwork.AutomaticallySyncScene = false;
        print(PhotonNetwork.AutomaticallySyncScene);
    }

    IEnumerator DisconnectAndLoadMenu()
    {
        print("desconectando");
        photonView.RPC("RPC_DisableSceneSync", RpcTarget.All); // Desativa sincronização globalmente
        print("dessincronizando e saindo da sala");
        PhotonNetwork.LeaveRoom();
        print("esperando sair da sala");
        while (PhotonNetwork.InRoom) yield return null; // Aguarda até sair da sala
        print("saiu da sala");
        if (SceneManager.GetActiveScene().name != "Menu")
        {
            print("Carregando Menu.");
            SceneManager.LoadScene("Menu");
        }
    }

    IEnumerator WaitToMenu()
    {
        yield return new WaitForSeconds(3);
        BackToMenu();
    }

    public void BackToMenu()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Menu");
    }
}
