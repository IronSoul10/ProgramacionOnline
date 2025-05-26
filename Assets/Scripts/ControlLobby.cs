using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ControlLobby : MonoBehaviourPunCallbacks
{
    #region PHOTON  

    public override void OnConnectedToMaster()
    {
        Conectado();
    }
    #endregion PHOTON  

    #region CANVAS - INICIO  
    [Header("Canvas - Inicio")]
    [SerializeField] private GameObject canvasInicio;
    [SerializeField] private TMP_InputField inputNickName;
    [SerializeField] private Button botonEntrar;
    [SerializeField] private TextMeshProUGUI notificacionesInicio;

    private void Start()
    {
        notificacionesInicio.text = "Conectandose...";

        botonEntrar.interactable = false;
        botonEntrar.onClick.AddListener(Entrar);

        PhotonNetwork.ConnectUsingSettings();
    }

    private void Conectado()
    {
        notificacionesInicio.text = "";

        Invoke("DelayConectando", 1);
    }

    private void DelayConectando()
    {
        botonEntrar.interactable = true;
    }

    private void Entrar()
    {
      
        if (!string.IsNullOrEmpty(inputNickName.text))
        {
            PhotonNetwork.NickName = inputNickName.text;
            notificacionesInicio.text = "Entrando al lobby...";
            
        }
        else
        {
            notificacionesInicio.text = "Por favor, ingresa un nombre de usuario.";
        }
    }
    #endregion CANVAS - INICIO  
}

