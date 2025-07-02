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

    public Image PersonajeImage
    {
        set
        {
            //si tenia una imagen instancia la borramos
            if(emptyImage.childCount > 0) Destroy(emptyImage.GetChild(0).gameObject);

            // Instanciamos la nueva imagen del personaje
            Instantiate(value, emptyImage);
        }
    }
}
