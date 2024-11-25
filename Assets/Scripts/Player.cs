using UnityEngine;
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

    [SerializeField] GameObject minimapCamera, emote;
    [SerializeField] Sprite portrait, dinoLostImage, dinoWinImage;
    [SerializeField] Sprite[] emotes;

    public List<Material> skins = new List<Material>(); // Lista de skins
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
        if (GameObject.FindGameObjectWithTag("Minimap") != null) GameObject.FindGameObjectWithTag("Minimap").GetComponent<RawImage>().texture = minimapTexture;
        if (GameController.Instance.portrait.sprite != null) GameController.Instance.portrait.sprite = this.portrait;

        if (GameController.Instance.healthBar != null) GameController.Instance.healthBar.BarValue = life;

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

        if (life <= 0) phView.RPC("RPC_EndGame", RpcTarget.All);

        if (Input.GetKeyDown(KeyCode.Alpha1)) phView.RPC("RPC_Emote", RpcTarget.All, 0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) phView.RPC("RPC_Emote", RpcTarget.All, 1);
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
    public void RPC_TakeDamage(float damage)
    {
        if (GetComponent<PhotonView>().IsMine)
        {
            gameObject.GetComponent<Player>().life -= damage;
            Debug.Log("Perdeu vida. Vida atual: " + gameObject.GetComponent<Player>().life);
        }
    }

    [PunRPC]
    public void RPC_EndGame()
    {
        GameController.Instance.endGameObject.SetActive(true);
        if (gameObject.CompareTag("Player")) GameController.Instance.endGameObject.GetComponent<Image>().sprite = dinoWinImage;
        else if (gameObject.CompareTag("TRex")) GameController.Instance.endGameObject.GetComponent<Image>().sprite = dinoLostImage;
        PhotonNetwork.LeaveRoom();
        StartCoroutine(WaitToMenu());
    }

    IEnumerator WaitToMenu()
    {
        yield return new WaitForSeconds(3);
        BackToMenu();
    }

    IEnumerator EmoteWait()
    {
        yield return new WaitForSeconds(2);
        emote.SetActive(false);
    }

    public void SkipCutscene()
    {
        phView.RPC("RPC_SkipCutscene", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_SkipCutscene()
    {
        GameController.Instance.skipCutsceneButton.SetActive(false);
        GameController.Instance._director.time = 58.5f;
    }

    public void ChangeSkin()
    {
        GetComponent<PhotonView>().RPC("RPC_ChangeSkin", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_ChangeSkin()
    {
        if (gameObject.CompareTag("Player"))
        {
            currentSkinIndex = (currentSkinIndex + 1) % skins.Count;
            Material newSkin = skins[currentSkinIndex];

            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (renderer != null) renderer.material = newSkin;
            }
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
    public void RPC_Emote(int index)
    {
        emote.GetComponent<Image>().sprite = emotes[index];
        emote.SetActive(true);
        
        StopCoroutine(EmoteWait());
        StartCoroutine(EmoteWait());
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
