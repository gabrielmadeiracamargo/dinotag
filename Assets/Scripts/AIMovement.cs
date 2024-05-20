using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMovement : MonoBehaviour
{
    Animator animator;

    public float moveSpeed, rotationSpeed = 100f; // Velocidade de rotação

    Vector3 stopPosition;

    float walkTime;
    public float walkCounter;
    float waitTime;
    public float waitCounter;

    int WalkDirection;
    Quaternion targetRotation;

    public bool isWalking;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        //So that all the prefabs don't move/stop at the same time
        walkTime = Random.Range(3, 6);
        waitTime = Random.Range(5, 7);

        waitCounter = waitTime;
        walkCounter = walkTime;

        ChooseDirection();
    }

    // Update is called once per frame
    void Update()
    {
        if (isWalking)
        {
            animator.SetBool("isRunning", true);

            walkCounter -= Time.deltaTime;

            // Rotaciona suavemente em direção ao alvo
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move para frente na direção que está enfrentando
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            if (walkCounter <= 0)
            {
                stopPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                isWalking = false;
                //stop movement
                transform.position = stopPosition;
                //reset the waitCounter
                waitCounter = waitTime;
            }
        }
        else
        {
            animator.SetBool("isRunning", false);
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                ChooseDirection();
            }
        }
    }

    public void ChooseDirection()
    {
        WalkDirection = Random.Range(0, 4);

        // Define a rotação alvo baseada na direção escolhida
        switch (WalkDirection)
        {
            case 0:
                targetRotation = Quaternion.Euler(0f, 0f, 0f);
                break;
            case 1:
                targetRotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            case 2:
                targetRotation = Quaternion.Euler(0f, -90f, 0f);
                break;
            case 3:
                targetRotation = Quaternion.Euler(0f, 180f, 0f);
                break;
        }

        isWalking = true;
        walkCounter = walkTime;
    }
}
