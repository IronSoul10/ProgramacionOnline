using UnityEditor.UI;
using UnityEngine;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    private static GameManager self;
    private void Awake()
    {
        self = this;
    }


}
