﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH"/>
// <summary>Demo code for Photon Chat in Unity.</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;


namespace Photon.Chat.Demo
{
    public class IgnoreUiRaycastWhenInactive : MonoBehaviour
    {
        // Implementation of the required interface method
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return this.gameObject.activeInHierarchy;
        }
    }
}