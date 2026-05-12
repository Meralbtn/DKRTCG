using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardGame;
using UnityEngine;
using UnityEngine.UI;
public class DeckConfirmPanelUI : MonoBehaviour
{
    private Deck _deck;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    public void Start()
    {
        _confirmButton.onClick.AddListener(OnConfirm);
        _cancelButton.onClick.AddListener(OnCancel);
    }
    public void Init(Deck deck)
    {
        _deck = deck;
    }

    private void ReSet()
    {
       _deck = null;
    }
    public void OnCancel()
    {
        ReSet();
        DeckEditManager.Instance.HideDeckEditPanel();
    }

    public async void OnConfirm()
    {
        await PlayerManager.Instance().DeleteDeckAsync(_deck);
        DeckEditManager.Instance.OnConfirmEdit();
    }
}
