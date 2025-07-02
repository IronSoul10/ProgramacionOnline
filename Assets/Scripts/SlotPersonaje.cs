using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


public class SlotPersonaje : MonoBehaviour,IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        
        ControlLobby.SeleccionPersonaje(gameObject.name);
    }
}
