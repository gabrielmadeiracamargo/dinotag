using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class SelectionManager : MonoBehaviour
{
    public float actualDistance, interactionDistance;
    public string targetName;

    public GameObject interactionInfoUI;
    TextMeshProUGUI interactionText;

    void Update()
    {
        if (!GetComponent<PhotonView>().IsMine) return;

        if (GameObject.Find("Canvas/Info Text") != null) 
        { 
            interactionInfoUI = GameObject.Find("Canvas/Info Text");
            interactionText = interactionInfoUI.GetComponent<TextMeshProUGUI>();
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit/*, 50*/))
        {
            var selectionTransform = hit.transform;
            actualDistance = Vector3.Distance(selectionTransform.position, transform.position);

            if (selectionTransform.GetComponent<InteractableObject>() && Vector3.Distance(selectionTransform.position, transform.position) < interactionDistance)
            {
                targetName = selectionTransform.name;
                interactionText.text = selectionTransform.GetComponent<InteractableObject>().GetItemName();
                interactionInfoUI.SetActive(true);
            }
            else
            {
                targetName = "";
                interactionInfoUI.SetActive(false);
            }

        }
        else interactionInfoUI.SetActive(false);
    }
}