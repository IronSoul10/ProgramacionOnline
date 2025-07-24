using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region CORE

    private static GameManager self;

    private void Awake()
    {
        self = this;
        Awake_ObtenerComponents();
        InstanciarJugador();
        Awake_Jugadores();
    }

    private void Start()
    {
        Start_Jugadores();
    }

    #endregion CORE

    #region COMPONENTES

    private Light2D luzGlobal; // using UnityEngine.Rendering.Universal;
    private Volume volumen;   // using UnityEngine.Rendering;
    private ColorAdjustments volumenColor;

    // Usage
    private void Awake_ObtenerComponents()
    {
        luzGlobal = GetComponent<Light2D>();
        volumen = GetComponent<Volume>();
        volumen.profile.TryGet(out volumenColor);
    }

    #endregion COMPONENTES

    #region PHOTON

    // frequently called IDO-1 usage:
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        fantasmas_jugador_PropiedadesCambiadas(targetPlayer);
    }

    #endregion PHOTON

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

    private void JugadorAsesinado(Player player)
    {
        //Removemos al player asesinado
        jugadoresVivos.Remove(asesinoPlayer);
    }

    #endregion JUGADORES

    #region FANTASMAS

    public void fantasmas_jugador_PropiedadesCambiadas(Player player)
    {
        //RETURN: Si no está guardado el jugador en el diccionario
        if (!dicJugadores.ContainsKey(player)) return;

        //Obtenemos al jugador del cual cambiaron sus propiedades
        Jugador jugador = dicJugadores[player];

        //Obtenemos si es un fantasma el Personaje que controlamos
        bool soyFantasma = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Fantasma");

        //- Si se convirtió en un fantasma
        if (player.CustomProperties.ContainsKey("Fantasma"))
        {
            //Propiedades del jugador
            jugador.Fantasma = true;

            //Este vivo o muerto, no me puede atravesar un fantasma
            jugador.Tangible = false;

            //Si soy un fantasma, puedo ver a los otros fantasmas. Si estoy vivo no veo fantasmas
            jugador.Opacidad = soyFantasma ? 0.25f : 0;

            //Instanciamos solo el Hijo que es el Sprite
            SpriteRenderer cadaver = Instantiate(jugador.Sprite, jugador.transform.position, Quaternion.identity);
            cadaver.color = Color.grey;
            cadaver.transform.rotation = Quaternion.Euler(x: 0, y: 0, z: 90);

            //Removemos los componentes que no son necesarios al cadaver
            Destroy(cadaver.gameObject.GetComponent<PhotonAnimatorView>());
            Destroy(cadaver.gameObject.GetComponent<Animator>());

            //Si yo me convertí en fantasma ...
            if (player == PhotonNetwork.LocalPlayer)
            {
                //PD: Si me volví fantasma, ya no puedo iniciar votación

                //Cuando estaba vivo no veía fantasmas
                //Pero si me convertí en fantasma ahora los debo de ver
                foreach (Jugador j in dicJugadores.Values)
                    if (j.Fantasma) j.Opacidad = 0.25f;
            }

            //Al final lo removemos de la lista y revisamos si aún quedan vivos
            JugadorAsesinado(player);
        }
    }

    #endregion FANTASMAS
}

