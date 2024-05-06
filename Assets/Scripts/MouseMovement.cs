using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity;
    public GameObject head;

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Movimento do mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotação da cabeça apenas no eixo X (para cima e para baixo)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotação do corpo apenas no eixo Y (para a esquerda e para a direita)
        yRotation += mouseX;

        // Aplicar rotação do corpo
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Aplicar rotação da cabeça
        head.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
