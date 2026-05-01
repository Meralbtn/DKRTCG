using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniCardZone : MonoBehaviour, IDropHandler
{
    public CardInstanceUI _instanceUI;
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("be attack");
    }
}
