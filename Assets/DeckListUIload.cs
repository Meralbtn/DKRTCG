using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame;

public class DeckListUIload : MonoBehaviour
{
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _deckItemPrefab;

    private void Start()
    {
        LoadDecks();
    }

    public void LoadDecks()
    {
        // 清空旧列表
        foreach (Transform child in _content)
            Destroy(child.gameObject);

        var decks = PlayerManager.Instance()._saveDecks;

        foreach (var deck in decks)
        {
            GameObject item = Instantiate(_deckItemPrefab, _content);
            item.GetComponent<DeckItemListUI>().Init(deck);  
        }
    }

    private void OnSelectDeck(Deck deck)
    {
        PlayerManager.Instance().SelectBattleDeck(deck);
        Debug.Log($"已选择卡组: {deck._deckName}");
    }
}