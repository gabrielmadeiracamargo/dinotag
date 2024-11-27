using System.Collections;
using System.Collections.Generic; // Necessário para List
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class GameTips : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI tipsText; // UI element to display tips
    [SerializeField] private List<string> tips = new List<string>(); // Lista dinâmica para dicas
    private int currentTipIndex = 0;

    private bool isTRex; // Determina se o jogador é um TRex
    private bool hasSword, hasGun; // Estado das armas para Player
    private string playerTag = "Player";
    private string trexTag = "TRex";

    void Start()
    {
        tipsText = GameObject.Find("Instruções").GetComponent<TextMeshProUGUI>();
        if (!photonView.IsMine)
        {
            Debug.Log("PhotonView não é deste jogador. Encerrando.");
            return;
        }

        if (tipsText == null)
        {
            Debug.LogError("TextMeshPro não está associado no campo 'tipsText'.");
            return;
        }

        Debug.Log("TextMeshPro associado corretamente.");

        isTRex = gameObject.CompareTag(trexTag);
        Debug.Log("O jogador é TRex? " + isTRex);

        UpdateTips(); // Configurar dicas no início
        Debug.Log("Dicas inicializadas.");

        StartCoroutine(DisplayTips());
        Debug.Log("Corrotina DisplayTips iniciada.");
    }

    private IEnumerator DisplayTips()
    {
        while (true)
        {
            if (tips.Count == 0)
            {
                Debug.LogWarning("Nenhuma dica disponível.");
                yield return new WaitForSeconds(4f);
                continue;
            }

            string currentTip = tips[currentTipIndex];
            tipsText.text = currentTip;
            Debug.Log($"Exibindo dica: {currentTip}");

            currentTipIndex = (currentTipIndex + 1) % tips.Count;
            yield return new WaitForSeconds(4f);
        }
    }

    public void UpdateTips()
    {
        print("atualizar");
        tips.Clear(); // Limpa as dicas antigas

        // Dicas gerais para todos
        tips.Add("Use WASD pra andar e espaço para pular");
        tips.Add("Jogue com 2 ou 3 jogadores");

        // Dicas específicas para armas
        if (!isTRex)
        {
            tips.Add("Derrote o dinossauro ou espere a ajuda chegar");

            if (PhotonNetwork.PlayerList.Length == 2)
            {
                tips.Add("Use scroll do mouse para trocar entre a espada e a arma sonífera");
            }

            if (hasGun)
            {
                tips.Add("Mire com o botão direito e atire com o esquerdo");
            }

            if (hasSword)
            {
                tips.Add("Clique esquerdo em seguida para atacar em combo");
            }
        }

        // Dicas para TRex
        if (isTRex)
        {
            tips.Add("Clique esquerdo em seguida para atacar em combo");
            tips.Add("Derrote o humano");
        }
    }

    public override void OnJoinedRoom()
    {
        UpdateTips(); // Atualiza dicas quando um jogador entra
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        UpdateTips();
    }

    public override void OnLeftRoom()
    {
        UpdateTips(); // Atualiza dicas quando um jogador sai
    }

    public void UpdateWeaponState(bool sword, bool gun)
    {
        hasSword = sword;
        hasGun = gun;
        UpdateTips(); // Reinitialize tips when weapon state changes
    }
}
