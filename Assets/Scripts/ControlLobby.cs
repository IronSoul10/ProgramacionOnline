using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
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

    public override void OnCreatedRoom()
    {
        
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        canvasInicio.SetActive(false);
        canvasSeleccion.SetActive(true);
        CargarSlotJugadores();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CrearSlotJugador(newPlayer);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        EliminarSlot(otherPlayer);
    }
    #endregion PHOTON  

    #region CANVAS - INICIO  
    [Header("\nCanvas - Inicio")]
    [SerializeField] private GameObject canvasInicio;
    [SerializeField] private TMP_InputField inputNickName;
    [SerializeField] private Button botonEntrar;
    [SerializeField] private TextMeshProUGUI notificacionesInicio;


    [Header("Canvas - Seleccion")]
    [SerializeField] private GameObject canvasSeleccion;


    #region SELECCION JUGADORES
    [Header("Seleccion Jugadores")]
    [SerializeField] private Transform panelJugadores;
    [SerializeField] private SlotJugador pfSlotJugador;

    private void Start()
    {
        canvasInicio.SetActive(true);
        canvasSeleccion.SetActive(false);

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

        if (!string.IsNullOrEmpty(inputNickName.text))
        {
            PhotonNetwork.NickName = inputNickName.text;
            notificacionesInicio.text = "Entrando al lobby...";

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

    #endregion CANVAS - INICIO  

    #region Canvas Seleccion Jugadores

    private static Dictionary<Player, SlotJugador> dicJugadores = new Dictionary<Player, SlotJugador>();

    private void CrearSlotJugador(Player player)
    {
        SlotJugador slot = Instantiate(pfSlotJugador, panelJugadores);

        slot.Player = player;

        dicJugadores.Add(player, slot);

    }

    private void CargarSlotJugadores()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            CrearSlotJugador(player);
        }
    }

    private void EliminarSlot(Player player)
    {
        SlotJugador slot = dicJugadores[player];
        dicJugadores.Remove(player);
        Destroy(slot.gameObject);

    }
}
#endregion

#endregion
