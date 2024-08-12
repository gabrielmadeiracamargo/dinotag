using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviourPunCallbacks
{
    public static LevelManager Instance;

    public GameObject _loaderCanvas;
    [SerializeField] private ProgressBar _progressBar;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public async void LoadScene()
    {
        _loaderCanvas.SetActive(true);

        do
        {
            if (_progressBar.BarValue <= 80) _progressBar.BarValue += Random.Range(10, 20); // variavel do progresso de carregamento de cena do photon é inacessível então eu só fiz uma barra fake mesmo
            else _progressBar.BarValue += Random.Range(2, 5); // deixando mais lento quando for demorar
            await Task.Delay(1000);
        } while (_progressBar.BarValue < 90f);
        PhotonNetwork.JoinRandomOrCreateRoom();

        if (PhotonNetwork.LevelLoadingProgress >= 97f)
        {
            _loaderCanvas.SetActive(false);
        }
    }
}
