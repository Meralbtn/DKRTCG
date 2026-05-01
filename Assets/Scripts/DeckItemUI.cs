using UnityEngine;
using TMPro;
using CardGame;

public class DeckItemUI : MonoBehaviour
{
    public TextMeshProUGUI _deckNameText;
    public TextMeshProUGUI _cardCountText;

    private Deck        _deck;
    private DeckListUI  _listUI;

    public void Init(Deck deck, DeckListUI listUI)
    {
        _deck   = deck;
        _listUI = listUI;

        _deckNameText.text  = deck._deckName;
        _cardCountText.text = $"{deck.GetCardCount()}/40";
    }

    // 编辑按钮
    public void OnClickEdit()
    {
        _listUI.SelectDeckForEdit(_deck);
    }

    // 删除按钮
    public void OnClickDelete()
    {
        _listUI.DeleteDeck(_deck);
    }

    // 选为战斗卡组按钮
    public void OnClickSelect()
    {
        PlayerManager.Instance().ChoseDeck(_deck._deckName);
        Debug.Log($"已选择战斗卡组: {_deck._deckName}");
    }
}