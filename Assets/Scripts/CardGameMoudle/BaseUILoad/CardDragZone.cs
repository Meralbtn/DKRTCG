using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame
{
    public class CardDragZone : MonoBehaviour,IDropHandler
    {
        public LoadTempDeck _update;
        public bool _isPile = false;
        public void OnDrop(PointerEventData eventData)
        {
            GameObject draggedObject = eventData.pointerDrag;
            CardDragHandler dragHandler = draggedObject.GetComponent<CardDragHandler>();
            if (dragHandler == null)
            {
                return;
            }
            if (!_isPile)
            {
                PlayerManager.Instance().AddCardToDeck(dragHandler.cardId);
                Debug.Log("AddCardToDeck");
            }
            else
            { 
                PlayerManager.Instance().DeleteCardToDeck(dragHandler.cardId);
                Debug.Log("DeleteCardToDeck");
            }
            _update.LoadTempDeckUI();
        }
    }
    
}