using Microsoft.Unity.VisualStudio.Editor;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
        Awake_Votacion();
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
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        fantasmas_jugador_PropiedadesCambiadas(targetPlayer);
    }
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Electricidad_Sala_PropiedadesCambiadas(propertiesThatChanged);
        Votacion_Sala_PropiedadesCambiadas(propertiesThatChanged);
    }

    #endregion PHOTON

    #region CAMARA
    [Header("Camara")]
    [SerializeField] private CinemachineCamera camara;
    #endregion CAMARA

    #region INICIO
    [Header("Inicio")]
    [SerializeField] private Transform spawnPoints;

    public static Jugador miJugador;

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

    #region ELECTRICIDAD

    public static Action<bool> OnElectricidadCambiada;
    private static bool _electricidad = true;

    public static bool Electricidad
    {
        get => _electricidad;
        set
        {
            Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
            propiedades["Electricidad"] = value;
            PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
        }
    }

    private void Electricidad_Sala_PropiedadesCambiadas(Hashtable propiedades)
    {
        // Si somos un fantasma, no nos afecta el cambio de luz
        if (miJugador.Fantasma) return;

        // Verifica que la llave exista
        if (!propiedades.ContainsKey("Electricidad")) return;

        // Obtenemos el valor
        _electricidad = (bool)propiedades["Electricidad"];

        // Apagamos o encendemos la luz del mapa
        luzGlobal.intensity = Electricidad ? 1 : 0;

        // Le quitamos el color si se apaga la luz
        volumenColor.saturation.value = Electricidad ? 0 : -100f;

        // Action
        OnElectricidadCambiada?.Invoke(Electricidad);
    }
    #endregion ELECTRICIDAD

    #region VIDEOVIGILANCIA

    [SerializeField] private GameObject ventanaVideovigilancia;
    [SerializeField] private GameObject videocamaras;

    private static bool _videovigilancia = false;

    public static bool Videovigilancia

    {
        get => _videovigilancia;
        set
        {
            _videovigilancia = value;
            self.ventanaVideovigilancia.SetActive(value);
            self.videocamaras.SetActive(value);
            miJugador.bloquearMovimiento = value;
        }
    }

    #endregion VIDEOVIGILANCIA

    #region VOTACION

    [Header("Votacion")]
    [SerializeField] private GameObject ventanaVotacion;
    [SerializeField] private Transform panelVotos;
    [SerializeField] private TMP_Text txtCuentaRegresiva;
    [SerializeField] private Voto pfVoto;

    private Dictionary<Player, Voto> dicVotos = new Dictionary<Player, Voto>();
    private static bool bloquearVoto = false;

    //Por quien votamos
    private static Player mivoto = null;

    private void Awake_Votacion()
    {
        //Por si dejamos la ventana abierta en el editor
        ventanaVotacion.SetActive(false);
    }

    public static void IniciarVotacion()
    {
        //Acreamos la propiedade de VotacionIniciada
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["VotacionIniciada"] = true;
        if (propiedades.ContainsKey("MasVotado")) propiedades.Remove(key: "MasVotado");
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        //Inicializar en "cero" los propiedades de la votación
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            //Reseteamos los votos a 0
            Hashtable pp = player.CustomProperties;
            pp["Votos"] = 0;
            player.SetCustomProperties(pp);
        }
        //Iniciamos la cuenta regresiva
        self.StartCoroutine(routine: CrCuentaRegresiva());
    }

    public static void Votar(Voto voto)
    {

    }

    //Para usar corrutinas, importar: using System.Collections;
    public static IEnumerator CrCuentaRegresiva()
    {
        //Tiempo inicial
        int t = 21;

    //Marcador
    RestarSegundo:

        t--; //Restamos 1 al tiempo

        //Lo aplicamos a las propiedades de la sala
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["CuentaRegresiva"] = t;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        //Esperamos 1 segundo
        yield return new WaitForSeconds(1f);

        //Si el tiempo aun no acaba, regresara el marcador
        if (t > 0) goto RestarSegundo;

        //Cuando termina la cuenta regresiva
        CuentaRegresivaFinalizada();
    }
    public static void CuentaRegresivaFinalizada()
    {

    }
    private void Votacion_Sala_PropiedadesCambiadas(Hashtable propiedades)
    {
        //- VOTACION INICIADA
        if (propiedades.ContainsKey("VotacionIniciada"))
        {
            //Obtenemos el value de la Key
            bool votacionIniciada = (bool)propiedades["VotacionIniciada"];

            //Abrimos la ventana de votacion
            VentanaVotacionAbierta = votacionIniciada;

            //Bloqueamos el movimiento de todos los jugadores
            if (votacionIniciada) bloquearMovimiento = true;
        }

        //- CUENTA REGRESIVA
        if (propiedades.ContainsKey("CuentaRegresiva"))
        {
            //Obtenemos el tiempo en segundos
            int t = (int)propiedades["CuentaRegresiva"];

            //Convertimos el tiempo al formato de cuenta regresiva
            TimeSpan timeSpan = TimeSpan.FromSeconds(t);
            txtCuentaRegresiva.text = timeSpan.ToString(format: @"mm\:ss");
        }
    }

    private bool VentanaVotacionAbierta
    {
        set
        {
            //RETURN: Si no hay cambios en el valor
            if (ventanaVotacion.activeSelf == value) return;

            //Encencer o Apagar la Ventana
            ventanaVotacion.SetActive(value);

            //Si se abrio la ventana
            if (value)
            {
                //Eliminamos todos los Slots que haya en el Panel Votos
                for (int i = 0; i < panelVotos.childCount; i++)
                    Destroy(panelVotos.GetChild(i).gameObject);

                //Creamos un nuevo diccionario
                dicVotos = new Dictionary<Player, Voto>();

                //Ciclamos todos los jugadores de la sala
                foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    //Instanciamos un voto
                    Voto voto = Instantiate(pfVoto, panelVotos);

                    //Le pasamos su Player
                    voto.Player = player;

                    //Agregamos el par al diccionario
                    dicVotos.Add(player, voto);

                    //Los que sean fantasmas, deshabilitamos su boton.
                    //O si nosotros somos un fantasma, deshabilitamos todos los botones (Votos)
                    if (player.CustomProperties.ContainsKey("Fantasma") || miJugador.Fantasma)
                        voto.Habilitado = false;
                }
            }
        }
    }


    // En el método ImagePersonaje, especifica el namespace completo para Image:
    public static UnityEngine.UI.Image ImagePersonaje(Player pLayer)
    {
        //Obtenemos el nombre del personaje que escogio el jugador
        string nombrePersonaje = pLayer.CustomProperties["Personaje"].ToString();

        //Obtenemos la ruta donde esta guardado el Personaje Image
        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} Image";

        //Retornamos el Prefab Image
        return Resources.Load<UnityEngine.UI.Image>(ruta);
    }

    #endregion RECUROS
}