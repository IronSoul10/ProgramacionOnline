using System;
using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Voto : MonoBehaviour
{
    [SerializeField] private TMP_Text nickname;
    [SerializeField] private Transform emptyImage;
    [SerializeField] private Transform panelVotantes;
    [SerializeField] private Button selfBoton;

    private void Awake()
    {
        //Le asignamos el metodo de botar al Boton
        selfBoton.onClick.AddListener(Votar);
    }

    private void Votar()
    {
        //Votamos
        GameManager.Votar(this);
    }

    public bool Habilitado
    {
        set => selfBoton.interactable = value;
    }

    public Player _player;
    public Player Player
    {
        get => _player;
        set
        {
            _player = value;
            nickname.text = value.NickName;

            // Obtener el prefab de la imagen del personaje
            Image imagePrefab = GameManager.ImagePersonaje(value);

            if (imagePrefab != null)
            {
                // Si el prefab existe, lo instanciamos
                Instantiate(imagePrefab, emptyImage);
            }
            else
            {
                // Si no existe, mostramos un error para depurar
                Debug.LogError($"Error: No se encontró la imagen del personaje para el jugador: {value.NickName}.");
            }
        }
    }


    public List<Player> Votantes
    {
        set
        {
            //Eliminamos los votantes que habia
            for (int i = 0; i < panelVotantes.childCount; i++)
                Destroy(panelVotantes.GetChild(i).gameObject);

            foreach (Player player in value)
            {
                //Obtenemos la Image del personaje que escogio el player
                Image pfImage = GameManager.ImagePersonaje(player);

                //Lo instanciamos en el Panel de Votantes
                Instantiate(pfImage, panelVotantes);
            }
        }
    }
}