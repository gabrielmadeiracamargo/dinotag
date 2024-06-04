using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(PhotonView))]

public class InteractableObject : MonoBehaviour
{
    public string ItemName;
    public SelectionManager selectionManager;

    public void Interact(/*PhotonView view*/)
    {
        GetComponent<PhotonView>().RPC("RPC_Interact", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_Interact()
    {
        Destroy(gameObject);
    }

    public void Awake()
    {
        selectionManager = GameObject.FindGameObjectWithTag("Player").GetComponent<SelectionManager>();
    }

    public string GetItemName()
    {
        return ItemName;
    }
    private void Update()
    {
        if (selectionManager.targetName == gameObject.name)
        {
            for (int i = 0; i < GetComponent<MeshRenderer>().materials.Length; i++)
            {
                GetComponent<MeshRenderer>().materials[i].SetColor("_OutlineColor", Color.white);
                GetComponent<MeshRenderer>().materials[i].SetFloat("_OutlineSize", 250);
            }

            if (Input.GetKeyDown(KeyCode.Mouse0)) Interact();
        }
        else
        {
            for (int i = 0; i < GetComponent<MeshRenderer>().materials.Length; i++)
            {
                GetComponent<MeshRenderer>().materials[i].SetColor("_OutlineColor", Color.black);
                GetComponent<MeshRenderer>().materials[i].SetFloat("_OutlineSize", 200);
            }
        }
    }
}