using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
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
        IniciarPartida();
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        canvasInicio.SetActive(false);
        canvasSeleccion.SetActive(true);

        //Cargamos todos los slots de jugadores
        CargarSlotJugadores();

        //Actualizamos el chat para mostrar el historial si somo nuevos
        ActualizarChat();

        //Control Spam
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
        // Actualizar el chat
        ActualizarChat();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Actualizar el personaje del jugador
        PersonajeActualizado(targetPlayer);
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

        //Botones
        botonEntrar.interactable = false;
        botonEntrar.onClick.AddListener(Entrar);
        botonEntrar.onClick.AddListener(EnviarMensaje);
        botonIniciarPartida.gameObject.SetActive(false);

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


    private void IniciarPartida()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["partida Iniciada"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
    }

    private void SalaCreada()
    {
        botonIniciarPartida.gameObject.SetActive(true);
        botonIniciarPartida.onClick.AddListener(IniciarPartida);
    }

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EnviarMensaje();
        }
    }
    private void EnviarMensaje()
    {
        if (mensajesEnviados >= 1) return;

        //Obtenemos el string del input field
        string mensaje = inputMensaje.text;

        // Verificamos que el mensaje no este vacio
        if (mensaje == string.Empty) return;

        //Limitamos el mensaje a 30 caracteres
        if (mensaje.Length > 30)
        {
            mensaje = mensaje.Substring(0, 30);
        }

        //Obtenemos propiedades de la sala
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        // Comprobacion
        string stringChat = propiedades.ContainsKey("Chat") && propiedades["Chat"] != null ? propiedades["Chat"].ToString() : "";

        //Comprobacion
        if (!string.IsNullOrEmpty(stringChat)) stringChat += "\n"; stringChat += $"{PhotonNetwork.NickName}: {mensaje}";

        ////Guardamos el value del pair con Key "Chat" como string
        //string stringChat = propiedades["Chat"].ToString();

        ////Conectamos nuestros mensajes al chat ya existente
        //stringChat += $"\n{PhotonNetwork.NickName}: {mensaje}";

        //Guardamos los cambios en el Hashtable
        propiedades["Chat"] = stringChat;

        //Aplicamnos los cambios al Photo
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        //Aumentamos la cantidad de mensajes enviados  
        mensajesEnviados++;

        //Limpiar el Imput
        inputMensaje.text = string.Empty;

        //Para recuperar el foco del Imput que se pierde al enviar el mensaje
        inputMensaje.ActivateInputField();

    }

    private void ActualizarChat()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!propiedades.ContainsKey("Chat")) return;

        //Obtenemos el string del chat
        string stringChat = propiedades["Chat"].ToString();

        //Asignamos el string al textChat
        textChat.text = stringChat;

        float offset = -textChat.rectTransform.anchoredPosition.y;

        int lineas = textChat.textInfo.lineCount + 1;

        float alturaLinea = -50f;

        float alturaTotal = lineas * alturaLinea + offset;

        content.sizeDelta = new Vector2(content.sizeDelta.x, alturaTotal);

        if (content.sizeDelta.y > scrollView.sizeDelta.y)
        {
            Vector3 posicionContent = content.localPosition;
            posicionContent.y = content.sizeDelta.y - scrollView.sizeDelta.y;
            content.localPosition = posicionContent;
        }

    }

    private IEnumerator CrControlSpam()
    {
    Inicio: //Marcador

        Debug.Log("Control de spawn Activado, espera 2 segundos");
        yield return new WaitForSeconds(2f); //espera 2 segundos

        if (mensajesEnviados > 0) //Si ha enviado mensaje
        {
            mensajesEnviados--; // Cada 2s le resta
            Debug.Log("Ya puedes enviar otro mensaje. mensajesEnviados = " + mensajesEnviados);
        }

        goto Inicio; //Regresar al Marcador
    }
    #endregion

    #region Canvas Seleccion Personaje

    public static void SeleccionPersonaje(string nombrePersonaje)
    {
        // Obtenemos las propiedades
        Hashtable propiedades = PhotonNetwork.LocalPlayer.CustomProperties;

        //Guardamos el nombre del personaje seleccionado
        propiedades["Personaje"] = nombrePersonaje;

        //Aplicamos los cambios al Photon
        PhotonNetwork.LocalPlayer.SetCustomProperties(propiedades);
    }

    public static void PersonajeActualizado(Player player)
    {
        // Obtenemos las propiedades
        Hashtable propiedades = PhotonNetwork.LocalPlayer.CustomProperties;

        //Return sino existe la propiedad
        if (!propiedades.ContainsKey("Personaje")) return;

        //Obtenemos el nombre del personaje
        string nombrePersonaje = propiedades["Personaje"].ToString();

        //Guardamos la ruta de donde esta el prefab del personaje
        string ruta = $"Personajes/{nombrePersonaje} /{nombrePersonaje} Image";

        //Obtenemos el prefab imagen del personaje
        Image personajeImage = Resources.Load<Image>(ruta);

        //Obtenemnos el SlotJugador del player
        SlotJugador slotJugador = dicJugadores[player];

        //Lo mostramos en pantalla
        slotJugador.PersonajeImage = personajeImage;
        #endregion

    }
}
