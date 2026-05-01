using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame;
using UnityEngine.SceneManagement;
public class DeckListUI  : MonoBehaviour
{
    public Transform _deckListContainer;  // 卡组列表的父节点
    public GameObject _deckItemPrefab;     // 卡组条目 Prefab
    public GameObject _createPanel;        // 新建卡组面板
    public TMP_InputField _deckNameInput;       // 新建卡组名输入框
    public TextMeshProUGUI _tipText;

    private void Start()
    {
        RefreshDeckList();
    }

    public void OnExitButtonClick()
    {
        SceneManager.LoadScene("MainMenu");
    } 
    // 刷新卡组列表
    public void RefreshDeckList()
    {
        foreach (Transform child in _deckListContainer)
            Destroy(child.gameObject);

        PlayerManager.Instance().GetAllSavedDecks();
        var decks = PlayerManager.Instance()._saveDecks;

        foreach (var deck in decks)
        {
            var item = Instantiate(_deckItemPrefab, _deckListContainer);
            var itemUI = item.GetComponent<DeckItemUI>();
            itemUI.Init(deck, this);
        }
    }

    // 点击新建按钮，弹出命名面板
    public void OnClickNewDeck()
    {
        _createPanel.SetActive(true);
        _deckNameInput.text = "";
    }

    // 确认新建
    public void OnClickConfirmCreate()
    {
        string deckName = _deckNameInput.text.Trim();
        if (string.IsNullOrEmpty(deckName))
        {
            _tipText.text = "卡组名不能为空";
            return;
        }

        // 检查名字是否重复
        var existing = PlayerManager.Instance()._saveDecks
            .Find(d => d._deckName == deckName);
        if (existing != null)
        {
            _tipText.text = "卡组名已存在";
            return;
        }

        // 创建新卡组并进入编辑场景
        PlayerManager.Instance().CreateNewDeck(deckName);
        _createPanel.SetActive(false);
        SceneManager.LoadScene("Deck");
    }

    // 选择已有卡组编辑
    public void SelectDeckForEdit(Deck deck)
    {
        PlayerManager.Instance().SelectDeckForEdit(deck);
        SceneManager.LoadScene("Deck");
    }

    // 删除卡组
    public void DeleteDeck(Deck deck)
    {
        PlayerManager.Instance().DeleteDeck(deck._deckName);
        RefreshDeckList();
    }

    public void OnClickCancel()
    {
        _createPanel.SetActive(false);
    }
}
