using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    #region CORE

    private static GameManager self;
    private void Awake()
    {
        self = this;
    }

    private void Start()
    {
        InstanciarJugador();
    }

    #endregion

    #region CAMARA

    [Header("CAMARA")]
    [SerializeField] private CinemachineCamera camara;
    public static CinemachineCamera Camara => self.camara;

    #endregion CAMARA

    #region INICIO

    [Header("INICIO")]
    [SerializeField] private Transform spawnPoints;
    private Jugador miLJugador;
    private void InstanciarJugador()
    {
        //Obtenemios el nombre del personaje que escogimos
        string nombreDePersonaje = PhotonNetwork.LocalPlayer.CustomProperties["Personaje"].ToString();

        //Obtenemos la ruta donde esta guardado el personaje InGame
        string ruta = $"Personajes/{nombreDePersonaje}/{nombreDePersonaje} InGame";

        //Obtenemos el spawnPoint
        int indice = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPoint = spawnPoints.GetChild(indice).position;

        //Instanciamos a nuestro personaje
        GameObject jugador = PhotonNetwork.Instantiate(ruta, spawnPoint, Quaternion.identity);

        //Guardamos la referencia de nuestro jugador
        miLJugador = jugador.GetComponent<Jugador>();
    }

    #endregion INICIO

}
