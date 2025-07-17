using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;


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

    private void Start()
    {
        canvasInicio.SetActive(true);
        canvasSeleccion.SetActive(false);

        notificacionesInicio.text = "Conectandose a PHOTON...";

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
        string nickName = inputNickName.text;

        //verificamos que no este vacio el nickname
        if (nickName == String.Empty)
        {
            notificacionesInicio.text = "El nickname esta vacio :C";
            return;
        }

        //Verificamos que no tenga mas de 10 caracteres
        if (nickName.Length > 10)
        {
            notificacionesInicio.text = "El nickname no puede tener mas de 10 caracteres";
            return;
        }

        //Gaurdamos nuestro nickname en Photon
        PhotonNetwork.NickName = nickName;

        notificacionesInicio.text = "Entrando al la Sala...";

        //Si no hay salas creadas
        if (PhotonNetwork.CountOfRooms == 0)
        {
            //Creamos las configuraciones de la sala
            var config = new RoomOptions() { MaxPlayers = 12 };

            //Intentamos crear una nueva sala
            bool conectado = PhotonNetwork.CreateRoom("XP", config);
            if (!conectado) notificacionesInicio.text = "No se pudo crear la sala :(";
        }
        else
        {
            //Intentamos unirnos a la sala
            bool conectado = PhotonNetwork.JoinRoom("XP");
            if (!conectado) notificacionesInicio.text = "No se pudo unir a la sala :(";
        }
    }

    #endregion CANVAS - INICIO

    #region CANVAS - SELECCION

    [Header("Canvas - Seleccion")]
    [SerializeField] private GameObject canvasSeleccion;
    [SerializeField] private Button botonIniciarPartida;
    private bool cargandoMapa = false;
    private void IniciarPartida()
    {
        //RETURN si ya le dimos anteriormente al boton
        if (cargandoMapa) return;

        //Indicamos que se esta cargando el mapa
        botonIniciarPartida.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Cargando Mapa...";

        //Obtenemos cuantos jugadores hay en la sal
        int numJugadores = PhotonNetwork.CurrentRoom.PlayerCount;

        //Importar la libreriade Random
        int indiceRandom = Random.Range(0,numJugadores);
        int i = 0;

        //Ciclar todos los jugadores
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            Hashtable pp = player.CustomProperties;

            //Sialguien no ha escogido personaje retornamos
            if (!pp.ContainsKey("Personaje")) return;

            //Si el indice es el indice random
            if(i == indiceRandom)
            {
                //Le crea la propiedad del asesino
                pp.Add("Asesino", true);
                player.SetCustomProperties(pp);
            }
            //Incrementamos el indice en 1
            i++;
        }

        //Iniciar en las propiedades que ya iniciamos la partida
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["PartidaIniciada"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        //Indicamos que empiece a cargar el mapa
        cargandoMapa = true;

        //Cargamos el mapa
        PhotonNetwork.LoadLevel("Mapa");
    }

    private void SalaCreada()
    {
        botonIniciarPartida.gameObject.SetActive(true);
        botonIniciarPartida.onClick.AddListener(IniciarPartida);
    }

    #region SELECCION JUGADORES

    [Header("Seleccion Jugadores")]
    [SerializeField] private Transform panelJugadores;
    [SerializeField] private SlotJugador pfSlotJugador;

    private static Dictionary<Player, SlotJugador> dicJugadores = new Dictionary<Player, SlotJugador>();

    private void CrearSlotJugador(Player player)
    {
        //Creamos el SlotJugador dentro de panelJugadores
        SlotJugador slot = Instantiate(pfSlotJugador, panelJugadores);

        //Le pasamos la referencia la jugador
        slot.Player = player;

        //Agregamos el par de Player/Slot al diccionario
        dicJugadores.Add(player, slot);

    }

    private void CargarSlotJugadores()
    {
        //Ciclamos a todos los jugadores que hay en la sala
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            CrearSlotJugador(player);
            PersonajeActualizado(player);
        }
    }

    private void EliminarSlot(Player player)
    {
        //Obtenemos el Slot a que esta ligado al Jugador
        SlotJugador slot = dicJugadores[player];

        //Eliminamos el par del diccionario, mediante la llave
        dicJugadores.Remove(player);

        //Destruimos el Slot del UI
        Destroy(slot.gameObject);

    }

    #endregion SELECCION JUGADORES

    #region SELECCION CHAT

    [Header("Chat")]
    [SerializeField] private RectTransform scrollView;
    [SerializeField] private RectTransform content;
    [SerializeField] private TextMeshProUGUI textChat;
    [SerializeField] private TMP_InputField inputMensaje;
    [SerializeField] private Button botonEnviar;
    private int mensajesEnviados = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) EnviarMensaje();
    }
    private void InicializarChat()
    {
        //Obtenemos las propiedades de la sala
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        //Agregamos un nuevo par a la Hashtable
        propiedades.Add("Chat", "Sala creada por: " + PhotonNetwork.NickName);

        //Aplicamos los cambios en Photon
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
    }

    private void EnviarMensaje()
    {
        //Control Spam
        if (mensajesEnviados >= 4) return;

        //Obtenemos el string del inputField
        string mensaje = inputMensaje.text;

        //Verificamos que el mensaje no este vacio
        if (mensaje == string.Empty) return;

        //Limitamos el mensaje a 30 caracteres
        if (mensaje.Length > 30)
        {
            mensaje = mensaje.Substring(0, 30);
        }

        //Obtenemos las propiedades de la sala
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        //Guardamos el value del par con key "Chat" como string
        string stringChat = propiedades["Chat"].ToString();

        //Conectamos nuestro mensaje al chat ya existente
        stringChat = $"\n {PhotonNetwork.NickName}: {mensaje}";

        //Guardamos los cambios en el Hashtable
        propiedades["Chat"] = stringChat;

        //Aplicamos los cambios en Photon
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        //aumentamos la cantidad de mensajes Enviados
        mensajesEnviados++;

        //Limpiar Input
        inputMensaje.text = string.Empty;

        //Para recuperar el foco del input que se pierde al enviar el mensaje
        inputMensaje.ActivateInputField();
    }

    private void ActualizarChat()
    {
        //Obtenemos las propiedades
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        //Verificamos que exista la llave Chat
        if (!propiedades.ContainsKey("Chat")) return;

        //Convertimos el Value a string
        string stringChat = propiedades["Chat"].ToString();

        //Actualizamos el TextMeshPro en pantalla
        textChat.text = stringChat;

        //Obtenemos el offset que tiene e txt chat con el Content
        float offset = textChat.rectTransform.anchoredPosition.y;

        //Obtenemos cuantas lineas lleva el chat
        int lineas = textChat.textInfo.lineCount + 1;

        //Obtenemos la altura de cada linea de texto
        float alturaLinea = 120f;

        //Obtenemos la altura total del texto
        float alturaTotal = lineas * alturaLinea + offset;

        //Asignamos la nueva altura al Content
        content.sizeDelta = new Vector2(content.sizeDelta.x, alturaTotal);

        //Movemos el content hasta abajo
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
        yield return new WaitForSeconds(2);

        if (mensajesEnviados > 0) //Si ha enviado mensajes
            mensajesEnviados--; //Cada 2s le resta

        goto Inicio; //Regresar al marcador

    }
    #endregion SELECCION CHAT

    #region SELECCION - PERSONAJES

    public static void SeleccionPersonaje(string nombrePersonaje)
    {
        //Obtenemos las propiedades
        Hashtable propiedades = PhotonNetwork.LocalPlayer.CustomProperties;

        //Guardamos el personaje que escogimos
        propiedades["Personaje"] = nombrePersonaje;

        //Aplicamos los cambios
        PhotonNetwork.LocalPlayer.SetCustomProperties(propiedades);
    }

    public static void PersonajeActualizado(Player player)
    {
        //Obtenemos las propiedades
        Hashtable propiedades = player.CustomProperties;

        //RETURN si no existe la llave
        if (!propiedades.ContainsKey("Personaje")) return;

        //Obtenemos el nombre del personaje que escogio el jugador
        string nombrePersonaje = propiedades["Personaje"].ToString();

        //Guardamos la ruta de donde esta guardado el Prefab
        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} Image";

        //Obtenemos el Prefab Image
        Image personajeImage = Resources.Load<Image>(ruta);

        //Obtenemos el Slot del player
        SlotJugador slotJugador = dicJugadores[player];

        //Lo mostramos en pantalla
        slotJugador.PersonajeImage = personajeImage;
    }

    #endregion SELECCION - PERSONAJES

    #endregion CANVAS - SELECCION
}