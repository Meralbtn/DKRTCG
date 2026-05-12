using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SingletonMonoModule;
using CardGame;
public class DeckEditManager : SingletonMono<DeckEditManager>
{
    [SerializeField] private GameObject _deckEditPanel;
    private DeckConfirmPanelUI _confirmPanel;
    [SerializeField] private DeckListUI _deckListUI;
    void Start()
    {
        _confirmPanel = _deckEditPanel.GetComponentInChildren<DeckConfirmPanelUI>();
    }
    public void ShowDeckEditPanel(Deck deck)
    {
        _confirmPanel.Init(deck);
        _deckEditPanel.SetActive(true);
    }

    public void HideDeckEditPanel()
    {
        _deckEditPanel.SetActive(false);
    }

    public void OnCancelEdit()
    {
        HideDeckEditPanel();
    }

    public void OnConfirmEdit()
    {
        HideDeckEditPanel();
        _deckListUI.RefreshDeckList();
    
    }
    
    public void OnExit()
    {
        DestroySelf();
    }
}
