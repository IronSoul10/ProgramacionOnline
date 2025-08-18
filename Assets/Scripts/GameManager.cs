using ExitGames.Client.Photon;
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
        Awake_ObtenerComponentes();
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

    private Light2D luzGlobal;
    private Volume volumen;
    private ColorAdjustments volumenColor;

    private void Awake_ObtenerComponentes()
    {
        luzGlobal = GetComponent<Light2D>();
        volumen = GetComponent<Volume>();
        volumen.profile.TryGet(out volumenColor);
    }

    #endregion COMPONENTES


    #region PHOTON

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        Fantasmas_Jugador_PropiedadesCambiadas(targetPlayer);
        Votacion_Jugador_PropiedadesCambiadas(targetPlayer);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Electricidad_Sala_PropiedadesCambiadas(propertiesThatChanged);
        Votacion_Sala_PropiedadesCambiadas(propertiesThatChanged);
    }

    #endregion PHOTON
    #region camara
    [Header("camara")]
    [SerializeField] private CinemachineCamera camara;
    #endregion

    #region INICIO
    [Header("Inicio")]
    [SerializeField] private Transform spawnPoints;

    private static Jugador miJugador;

    private void InstanciarJugador()
    {
        string nombrePersonaje = PhotonNetwork.LocalPlayer.CustomProperties["Personaje"].ToString();

        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} InGame";

        int indice = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPoint = spawnPoints.GetChild(indice).position;

        GameObject jugador = PhotonNetwork.Instantiate(ruta, spawnPoint, Quaternion.identity);

        miJugador = jugador.GetComponent<Jugador>();

        camara.Follow = miJugador.transform;
    }

    #endregion INICIO

    #region Jugadores

    #region JUGADORES
    private Dictionary<Player, Jugador> dicJugadores = new Dictionary<Player, Jugador>();
    private List<Player> jugadoresVivos = new List<Player>();
    private static Player asesinoPlayer;
    public static bool bloquearMovimiento = false;

    private void Awake_Jugadores()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.CustomProperties.ContainsKey("Asesino")) asesinoPlayer = player;
            else jugadoresVivos.Add(player);
        }
    }

    private void Start_Jugadores()
    {
        Invoke("BuscarJugadores", 1);
    }

    private void BuscarJugadores()
    {
        var busqueda = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject go in busqueda)
        {
            Jugador jugador = go.GetComponent<Jugador>();
            dicJugadores.Add(jugador.Player, jugador);
        }
    }

    private void JugadorAsesinado(Player player)
    {
        jugadoresVivos.Remove(player);
    }
    #endregion JUGADORES


    #endregion

    #region Canvas
    [Header("Canvas")]
    [SerializeField] private TMP_Text txtCentral;

    public static void MostrarTxtCentral(string texto)
    {
        self.txtCentral.text = texto;
        self.txtCentral.gameObject.SetActive(true);
        self.Invoke(methodName: "OcultarTextoCentral", time: 2f);
    }

    private void OcultarTextoCentral()
    {
        txtCentral.gameObject.SetActive(false);
    }
    #endregion Canvas

    #region FANTASMAS
    public void Fantasmas_Jugador_PropiedadesCambiadas(Player player)
    {
        if (!dicJugadores.ContainsKey(player)) return;

        Jugador jugador = dicJugadores[player];

        bool soyFantasma = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Fantasma");


        if (player.CustomProperties.ContainsKey("Fantasma"))
        {

            jugador.Fantasma = true;

            jugador.Tangible = false;

            jugador.Opacidad = soyFantasma ? 0.25f : 0;


            SpriteRenderer cadaver = Instantiate(jugador.Sprite, jugador.transform.position, Quaternion.identity);
            cadaver.color = Color.grey;
            cadaver.transform.rotation = Quaternion.Euler(x: 0, y: 0, z: 90);


            Destroy(obj: cadaver.gameObject.GetComponent<PhotonAnimatorView>());
            Destroy(obj: cadaver.gameObject.GetComponent<Animator>());
        }


        if (player == PhotonNetwork.LocalPlayer)
        {

            foreach (Jugador j in dicJugadores.Values)
                if (j.Fantasma) j.Opacidad = 0.25f;
        }


        JugadorAsesinado(player);
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
        if (miJugador.Fantasma) return;

        if (!propiedades.ContainsKey("Electricidad")) return;

        _electricidad = (bool)propiedades["Electricidad"];

        luzGlobal.intensity = Electricidad ? 1 : 0;

        volumenColor.saturation.value = Electricidad ? 0 : -100f;

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

    private static Player miVoto = null;

    private void Awake_Votacion()
    {
        ventanaVotacion.SetActive(false);
    }

    public static void IniciarVotacion()
    {
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["VotacionIniciada"] = true;

        if (propiedades.ContainsKey("MasVotado")) propiedades.Remove("MasVotado");


        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            Hashtable pp = player.CustomProperties;
            pp["Votantes"] = string.Empty;
            pp["Votos"] = 0;
            player.SetCustomProperties(pp);
        }

        self.StartCoroutine(CrCuentaRegresiva());
    }

    public static void Votar(Voto voto)
    {
        if (bloquearVoto) return;

        bloquearVoto = true;
        self.Invoke(nameof(DesbloquearVoto), time: 0.5f);

        Hashtable propiedades = voto.Player.CustomProperties;

        List<Player> votantes = StringToList(propiedades["Votantes"].ToString());

        votantes.Add(PhotonNetwork.LocalPlayer);

        propiedades["Votantes"] = ListToString(votantes);

        propiedades["Votos"] = (int)propiedades["Votos"] + 1;

        voto.Player.SetCustomProperties(propiedades);

        if (miVoto != null)
        {
            Hashtable p = miVoto.CustomProperties;

            votantes = StringToList(p["Votantes"].ToString());

            votantes.Remove(PhotonNetwork.LocalPlayer);

            p["Votantes"] = ListToString(votantes);

            p["Votos"] = (int)p["Votos"] - 1;

            miVoto.SetCustomProperties(p);
        }

        miVoto = voto.Player;
    }

    public void DesbloquearVoto()
    {
        bloquearVoto = false;
    }

    public static IEnumerator CrCuentaRegresiva()
    {
        int t = 21;

    RestarSegundo:

        t--;
        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;
        propiedades["CuentaRegresiva"] = t;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);

        yield return new WaitForSeconds(1f);

        if (t > 0) goto RestarSegundo;

        CuentaRegresivaFinalizada();
    }

    private static void CuentaRegresivaFinalizada()
    {

        Hashtable propiedades = PhotonNetwork.CurrentRoom.CustomProperties;

        int mayor = -1;
        Player masVotado = null;
        bool empate = false;

        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            int votos = (int)player.CustomProperties["Votos"];

            if (votos > mayor)
            {
                mayor = votos;
                masVotado = player;
                empate = false;
            }
            else if (votos == mayor)
            {
                empate = true;
            }
        }

        propiedades["VotacionIniciada"] = false;

        propiedades.Remove("CuentaRegresiva");

        propiedades["MasVotado"] = empate ? -1 : masVotado.ActorNumber;

        PhotonNetwork.CurrentRoom.SetCustomProperties(propiedades);
    }

    private void Votacion_Sala_PropiedadesCambiadas(Hashtable propiedades)
    {

        if (propiedades.ContainsKey("VotacionIniciada"))
        {
            bool votacionIniciada = (bool)propiedades["VotacionIniciada"];

            VentanaVotacionAbierta = votacionIniciada;

            if (votacionIniciada) bloquearMovimiento = true;
        }

        if (propiedades.ContainsKey("CuentaRegresiva"))
        {
            int t = (int)propiedades["CuentaRegresiva"];

            TimeSpan timeSpan = TimeSpan.FromSeconds(t);
            txtCuentaRegresiva.text = timeSpan.ToString(@"mm\:ss");
        }

        if (propiedades.ContainsKey("MasVotado"))
        {
            //Obtenemos el valor
            int actorNumber = (int)propiedades["MasVotado"];

            //Empate
            if (actorNumber == -1)
            {
                //Vamos a mostrar el texto de empate
                MostrarTxtCentral(texto: "Empate de Votacion");

                //Desbloquear el movimiento de los jugadores
                bloquearMovimiento = false;
            }
            else
            {
                //Expulsamos al jugador
                StartCoroutine(routine: CrExpulsar(actorNumber));
            }
        }
    }

    private bool VentanaVotacionAbierta
    {
        set
        {
            if (ventanaVotacion.activeSelf == value) return;

            ventanaVotacion.SetActive(value);

            if (value)
            {
                for (int i = 0; i < panelVotos.childCount; i++)
                    Destroy(panelVotos.GetChild(i).gameObject);

                dicVotos = new Dictionary<Player, Voto>();

                foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    Voto voto = Instantiate(pfVoto, panelVotos);
                    voto.gameObject.SetActive(true);

                    voto.Player = player;

                    dicVotos.Add(player, voto);


                    if (player.CustomProperties.ContainsKey("Fantasma") || miJugador.Fantasma)
                        voto.Habilitado = false;
                }
            }
        }

    }

    public static string ListToString(List<Player> listaPlayers)
    {
        string cadena = "";

        foreach (Player player in listaPlayers)
            cadena += $"{player.ActorNumber}-";

        return cadena;
    }

    public static List<Player> StringToList(string cadena)
    {
        List<string> listaIds = new List<string>(cadena.Split('-'));

        List<Player> listaPlayers = new List<Player>();

        foreach (string idString in listaIds)
        {
            if (idString == string.Empty) continue;

            int id = Convert.ToInt32(idString);

            Player player = PhotonNetwork.CurrentRoom.GetPlayer(id);

            listaPlayers.Add(player);
        }

        return listaPlayers;
    }
    private void Votacion_Jugador_PropiedadesCambiadas(Player player)
    {

        Hashtable propiedades = player.CustomProperties;

        if (!propiedades.ContainsKey("Votantes")) return;

        string votantes = propiedades["Votantes"].ToString();

        dicVotos[player].Votantes = StringToList(votantes);
    }

    #endregion VOTACION

    #region Expulsor

    [Header("Expulsor")]
    [SerializeField] private Animator expulsor;
    [SerializeField] private Transform emptyPersonaje;

    public IEnumerator CrExpulsar(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        Jugador jugador = dicJugadores[player];

        string txt = jugador.Asesino ? $"{player.NickName} era el Asesino" : $"{player.NickName} no era el Asesino";
        MostrarTxtCentral(txt);

        jugador.PhotonTransform.enabled = false;
        jugador.transform.SetParent(emptyPersonaje);
        jugador.transform.position = emptyPersonaje.position;

        expulsor.gameObject.SetActive(true);
        expulsor.SetTrigger("expulsar");

        yield return new WaitForSeconds(5);

        if (jugador.Asesino)
        {
            yield break;
        }

        expulsor.gameObject.SetActive(false);

        jugador.transform.SetParent(null);

        jugador.transform.position = Vector3.zero;

        jugador.transform.localScale = Vector3.one;

        jugador.PhotonTransform.enabled = true;

        if (miJugador.Player.ActorNumber == player.ActorNumber)
        {
            Hashtable propiedadesJugador = player.CustomProperties;
            propiedadesJugador["Fantasma"] = true;
            player.SetCustomProperties(propiedadesJugador);
        }

        bloquearMovimiento = false;
    }
    #endregion


    #region RECUROS

    public static Image ImagePersonaje(Player player)
    {
        string nombrePersonaje = player.CustomProperties["Personaje"].ToString();

        string ruta = $"Personajes/{nombrePersonaje}/{nombrePersonaje} Image";

        return Resources.Load<Image>(ruta);
    }

    #endregion RECUROS

}