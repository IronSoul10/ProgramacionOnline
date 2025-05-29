using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class SlotJugador : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textNickname;
    [SerializeField] private Transform emptyImage;

    public Player Player 
    {
        set
        {
            // Mostrar el nombre del jugador
            textNickname.text = value.NickName;
        }
    }
}
