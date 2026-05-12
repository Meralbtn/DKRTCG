using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;

public class DeckItemListUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _deckNameText;
    private Deck _deck;

    public void Init(Deck deck)
    {
        _deck = deck;
        _deckNameText.text = deck._deckName;
    }
    public void OnClickEdit()
    {
        PlayerManager.Instance().SelectBattleDeck(_deck);
        //关闭展示卡牌
        transform.GetComponentInParent<DeckListUIload>().gameObject.SetActive(false);
    }
}
