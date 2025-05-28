using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlLobby : MonoBehaviourPunCallbacks
{
    #region PHOTON  

    public override void OnConnectedToMaster()
    {
        Conectado();
    }
    #endregion PHOTON  

    #region CANVAS - INICIO  
    [Header("\nCanvas - Inicio")]
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

        Invoke("DelayConectado", 1);
    }

    private void DelayConectado()
    {
        botonEntrar.interactable = true;
    }

    private void Entrar()
    {
        string nickName = inputNickName.text.Trim();
        if (string.IsNullOrEmpty(nickName))
        {
            notificacionesInicio.text = "El nombre de usuario no puede estar vacío.";
            return;
        }

        if (nickName.Length < 3 || nickName.Length > 10)
        {
            notificacionesInicio.text = "El nombre de usuario debe tener entre 3 y 10 caracteres.";
            return;
        }

        PhotonNetwork.NickName = nickName;

        if (PhotonNetwork.CountOfRooms == 0)
        {
            notificacionesInicio.text = "Creando nueva sala...";

            var conf = new RoomOptions() { MaxPlayers = 10 };

            bool conectando = PhotonNetwork.CreateRoom("XP", conf);

            if (!conectando) notificacionesInicio.text = "No se pudo conectar a la sala.";
        }
        else
        {
            notificacionesInicio.text = "Uniéndose a la sala...";
            bool conectando = PhotonNetwork.JoinRoom("XP");
            if (!conectando) notificacionesInicio.text = "No se pudo conectar a la sala.";
        }
    }
}
#endregion

//Conectar a Photon automáticamente al iniciar la escena 

//Evitar que se pueda conectar si el Nickname está vacío o tiene más de 10 caracteres

//Mostrar en las notificaciones de Conexión a Photon y filtro de Nickname
