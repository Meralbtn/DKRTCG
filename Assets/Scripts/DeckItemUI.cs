using UnityEngine;
using TMPro;
using CardGame;
using UnityEngine.EventSystems;

public class DeckItemUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI _deckNameText;
    public TextMeshProUGUI _cardCountText;

    private Deck _deck;
    private DeckListUI  _listUI;

    public void Init(Deck deck, DeckListUI listUI)
    {
        _deck   = deck;
        _listUI = listUI;

        _deckNameText.text  = deck._deckName;
        _cardCountText.text = $"{deck.GetCardCount()}/40";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            DeckEditManager.Instance.ShowDeckEditPanel(_deck);
        }
    }

}