using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region CORE

    private static GameManager self;

    private void Awake()
    {
        self = this;
        InstanciarJugador();
        Awake_Jugadores();
    }

    private void Start()
    {
        Start_Jugadores();
    }

    #endregion CORE

    #region CAMARA
    [Header("Camara")]
    [SerializeField] private CinemachineCamera camara;
    #endregion CAMARA

    #region INICIO
    [Header("Inicio")]
    [SerializeField] private Transform spawnPoints;

    private Jugador miJugador;

    private void InstanciarJugador()
    {
        // Obtenemos el nombre del personaje que escogimos
        string nombrePersonaje = PhotonNetwork.LocalPlayer.CustomProperties["Personaje"].ToString();

        // Obtenemos la ruta donde esta guardado el Personaje InGame
        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} InGame";

        // Obtenemos el spawnPoint
        int indice = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPoint = spawnPoints.GetChild(indice).position;

        // Instanciamos a nuestro Personaje
        GameObject jugador = PhotonNetwork.Instantiate(ruta, spawnPoint, Quaternion.identity);

        // Guardamos la referencia de nuestro jugador
        miJugador = jugador.GetComponent<Jugador>();

        // A nuestro jugador hacemos que lo siga la camara
        camara.Follow = miJugador.transform;
    }
    #endregion INICIO

    #region CANVAS
    [Header("Canvas")]
    [SerializeField] private TMP_Text txtCentral;

    public static void MostrarTxtCentral(string texto)
    {
        self.txtCentral.text = texto;
        self.txtCentral.gameObject.SetActive(true);
        self.Invoke("OcultarTextoCentral", 2f);
    }

    private void OcultarTextoCentral()
    {
        txtCentral.gameObject.SetActive(false);
    }
    #endregion CANVAS

    #region JUGADORES
    private Dictionary<Player, Jugador> dicJugadores = new Dictionary<Player, Jugador>();
    private List<Player> jugadoresVivos = new List<Player>();
    public static Player asesinoPlayer;
    public static bool bloquearMovimiento = false;

    private void Awake_Jugadores()
    {
        // Ciclamos todos los jugadores
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            //Guardar quien es el asesino
            if (player.CustomProperties.ContainsKey("Asesino")) asesinoPlayer = player;
            //Si no es el asesino
            else jugadoresVivos.Add(player);
        }
    }

    private void Start_Jugadores()
    {
        //Damos un Delay de 1s para que de tiempo a Photon instanciar a todos los jugadores
        Invoke("BuscarJugadores", 1);
    }

    private void BuscarJugadores()
    {
        //Obtenemos a todos los Jugadores
        var busqueda = GameObject.FindGameObjectsWithTag("Player");

        //Los agregamos al diccionario
        foreach (GameObject go in busqueda)
        {
            //Agregamos el par al diccionario
            Jugador jugador = go.GetComponent<Jugador>();
            dicJugadores.Add(jugador.Player, jugador);
        }
    }
    #endregion JUGADORES
}

