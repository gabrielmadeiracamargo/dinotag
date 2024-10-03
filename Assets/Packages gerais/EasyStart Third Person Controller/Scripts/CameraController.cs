using UnityEngine;
using Photon.Pun;

public class CameraController : MonoBehaviour
{
    public bool clickToMoveCamera = false;
    public bool canZoom = true;
    public float sensitivity = 5f;
    public Vector2 cameraLimit = new Vector2(-45, 40);

    float mouseX;
    float mouseY;
    float offsetDistanceY;

    Transform player;

    void Start()
    {
        if (!GetComponentInParent<PhotonView>().IsMine) return;
        player = gameObject.transform.parent.gameObject.transform;
        offsetDistanceY = transform.position.y;

        // Lock and hide cursor if not using clickToMoveCamera
        if (!clickToMoveCamera)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!GetComponentInParent<PhotonView>().IsMine) return;

        // Camera follows player with an offset
        transform.position = player.position + new Vector3(0, offsetDistanceY, 0);

        // Zoom control
        if (canZoom && Input.GetAxis("Mouse ScrollWheel") != 0)
            Camera.main.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * sensitivity * 2;

        // Right-click camera movement (if enabled)
        if (clickToMoveCamera && Input.GetAxisRaw("Fire2") == 0)
            return;

        // Calculate mouse input
        mouseX += Input.GetAxis("Mouse X") * sensitivity;
        mouseY += Input.GetAxis("Mouse Y") * sensitivity;
        mouseY = Mathf.Clamp(mouseY, cameraLimit.x, cameraLimit.y); // Apply vertical camera limits

        // Rotate the camera based on mouse input
        transform.rotation = Quaternion.Euler(-mouseY, mouseX, 0);

        // Rotate the player based on the mouse's horizontal movement
        player.rotation = Quaternion.Euler(0, mouseX, 0); // Only rotate around Y axis for the player
    }
}
