using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame;

public class DeckSlotButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _deckNameText;
    [SerializeField] private GameObject _deckListPanel;
    private void Start()
    {
        UpdateDisplay();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        _deckListPanel.SetActive(true);
        DeckListUIload deckListUI = _deckListPanel.GetComponent<DeckListUIload>();
        if (deckListUI != null)
        {
            deckListUI.LoadDecks();
        }
    }
    void Update()
    {
        UpdateDisplay();
    }
    public void UpdateDisplay()
    {
        var deck = PlayerManager.Instance().GetBattleDeck();
        if (_deckNameText != null)
            _deckNameText.text = deck?._deckName ?? "默认卡组";
    }
}