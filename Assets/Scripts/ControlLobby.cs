using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class ControlLobby : MonoBehaviourPunCallbacks
{
    #region PHOTON  

    public override void OnConnectedToMaster()
    {
        Conectado();
    }

    public override void OnCreatedRoom()
    {
        SalaCreada();
        InicializarChat();
    }

    public override void OnJoinedRoom()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        if (propiedades.ContainsKey("PartidaIniciada"))
        {
            partidaIniciada = true;
            PhotonNetwork.LeaveRoom();
            notificacionesInicio.text = "la partida ya esta iniciada";
            return;
        }
        PhotonNetwork.AutomaticallySyncScene = true;
        canvasInicio.SetActive(false);
        canvasSeleccion.SetActive(true);
        CargarSlotJugadores();
        ActualizarChat();
        StartCoroutine(CrControlSpam());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CrearSlotJugador(newPlayer);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        EliminarSlot(otherPlayer);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        ActualizarChat();
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        PersonajeActualizado(targetPlayer);
    }


    #endregion PHOTON  

    #region CANVAS - INICIO  
    [Header("\nCanvas - Inicio")]
    [SerializeField] private GameObject canvasInicio;
    [SerializeField] private TMP_InputField inputNickName;
    [SerializeField] private Button botonEntrar;
    [SerializeField] private TextMeshProUGUI notificacionesInicio;
    private bool partidaIniciada = false;


    [Header("Canvas - Seleccion")]
    [SerializeField] private GameObject canvasSeleccion;
    [SerializeField] private Button botonIniciarPartida;


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
        botonEnviar.onClick.AddListener(EnviarMensaje);
        botonIniciarPartida.gameObject.SetActive(false);

        PhotonNetwork.ConnectUsingSettings();
    }

    private void Conectado()
    {
        if (!partidaIniciada)
        {
            notificacionesInicio.text = "";

            Invoke("DelayConectado", 1);
        }

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

    private void IniciarPartida()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["PartidaIniciada"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
    }

    private void SalaCreada()
    {
        botonIniciarPartida.gameObject.SetActive(true);
        botonIniciarPartida.onClick.AddListener(IniciarPartida);
    }


    #endregion CANVAS - INICIO  

    #region Canvas Seleccion Jugadores

    [Header("Jugadores")]
    [SerializeField] private SlotJugador pfslotJugador;

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
            PersonajeActualizado(player);
        }
    }

    private void EliminarSlot(Player player)
    {
        SlotJugador slot = dicJugadores[player];
        dicJugadores.Remove(player);
        Destroy(slot.gameObject);

    }

    #endregion

    #endregion

    #region CHAT
    [Header("Chat")]
    [SerializeField] private RectTransform scrollView;
    [SerializeField] private RectTransform content;
    [SerializeField] private TextMeshProUGUI textChat;
    [SerializeField] private TMP_InputField inputMensaje;
    [SerializeField] private Button botonEnviar;
    private int mensajesEnviados = 0;


    private void InicializarChat()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades.Add("Chat", "Sala creada por: " + PhotonNetwork.NickName);
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EnviarMensaje();
        }
    }
    private void EnviarMensaje()
    {
        if (mensajesEnviados >= 4) return;

        string mensaje = inputMensaje.text;

        if (mensaje == string.Empty) return;

        if (mensaje.Length > 30)
        {
            mensaje = mensaje.Substring(0, 30);
        }

        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        string stringChat = propiedades.ContainsKey("Chat") ? propiedades["Chat"] as string : "";

        if (!string.IsNullOrEmpty(stringChat))
            stringChat += "\n";

        stringChat += $"{PhotonNetwork.NickName}: {mensaje}";

        propiedades["Chat"] = stringChat;

        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        mensajesEnviados++;
        inputMensaje.text = string.Empty;

        inputMensaje.ActivateInputField();

        ActualizarChat();

    }

    private void ActualizarChat()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        if (propiedades.ContainsKey("Chat"))
        {
            string stringChat = propiedades["Chat"] as string;

            textChat.text = stringChat;

            float offset = textChat.rectTransform.anchoredPosition.y;

            int lineas = textChat.textInfo.lineCount + 1;

            float alturaLinea = 55f;

            float alturaTotal = lineas * alturaLinea + offset;

            content.sizeDelta = new Vector2(content.sizeDelta.x, alturaTotal);

            if (content.sizeDelta.y > scrollView.sizeDelta.y)
            {
                Vector3 posicionContent = content.localPosition;
                posicionContent.y = content.sizeDelta.y - scrollView.sizeDelta.y;
                content.localPosition = posicionContent;
            }

        }
    }

    private IEnumerator CrControlSpam()
    {
    Inicio:
        yield return new WaitForSeconds(2);

        if (mensajesEnviados > 0)
            mensajesEnviados--;

        goto Inicio;

    }
    #endregion

    #region SELECTION - Personajes

    public static void SeleccionPersonaje(string nombrePersonaje)
    {

        Hashtable propiedades = PhotonNetwork.LocalPlayer.CustomProperties;

        propiedades["Personaje"] = nombrePersonaje;

        PhotonNetwork.LocalPlayer.SetCustomProperties(propiedades);
    }

    public static void PersonajeActualizado(Player player)
    {
        Hashtable propiedades = player.CustomProperties;

        if (!propiedades.ContainsKey("Personaje")) return;

        string nombrePersonaje = propiedades["Personaje"].ToString();

        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} Image";

        Image personajeImage = Resources.Load<Image>(ruta);

        SlotJugador slotJugador = dicJugadores[player];

        slotJugador.PersonajeImage = personajeImage;
    }

    #endregion SELECTION - Personajes


}