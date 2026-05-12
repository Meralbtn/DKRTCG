using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using NUnit.Framework;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using DG.Tweening;
using CardGameClient;
using System.Linq;
//希望可以悬停显示,写在minicard中
//实现minicard的居中布局
public class MiniCardGridManager : MonoBehaviour
{
    //获取PREFAB组件

    #region TestValue
    public GameObject _miniCardPrefab;
    public int _miniCardWidth = 150;
    public int _spacing = 10;
    private Dictionary<int, CardInstanceUI> _activeInstances = new Dictionary<int, CardInstanceUI>();
    private List<CardInstanceUI> _cardPool = new List<CardInstanceUI>();
    private List<CardInstanceUI> _activeCards = new List<CardInstanceUI>();
    private int _count = 0;
    public BattleInfo _user;
    #endregion

    public int GetMiniCardCount() => _activeInstances.Count;

    public IReadOnlyList<CardInstanceUI> GetActiveCards() => _activeCards;

    public void SyncFromServer(List<MiniCardState> serverCards, bool isEnemy = false)
    {
        if (serverCards == null) serverCards = new List<MiniCardState>();
        //删除服务器已不存在的随从
        var serverIds = new HashSet<int>(serverCards.Select(c => c.InstanceId));
        var toRemove = _activeInstances
            .Where(kv => !serverIds.Contains(kv.Key))
            .Select(kv => kv.Key)
            .ToList();

        foreach (var id in toRemove)
        {
            var inst = _activeInstances[id];
            _activeInstances.Remove(id);
            HandleCardRemove(inst);
        }

        // 2. 新增服务器有但本地没有的随从
        foreach (var sc in serverCards)
        {
            if (!_activeInstances.ContainsKey(sc.InstanceId))
            {
                Card cardData = PlayerManager.Instance().GetCardList()[sc.CardId];
                CardInstanceUI cardScript = CreateCardInstance(cardData, sc.InstanceId);
                _activeInstances[sc.InstanceId] = cardScript;
            }

            var inst = _activeInstances[sc.InstanceId];
            inst.UpdateStats(sc.HP, sc.Attack, sc.AttackUsed);
        }

        SortLayout();
    }

    private CardInstanceUI CreateCardInstance(Card card, int instanceId)
    {
        CardInstanceUI cardScript;

        if (_cardPool.Count > 0)
        {
            cardScript = _cardPool[0];
            _cardPool.RemoveAt(0);
            cardScript.gameObject.SetActive(true);
            cardScript.transform.SetParent(this.transform);
        }
        else
        {
            GameObject cardObj = Instantiate(_miniCardPrefab, transform, false);
            cardScript = cardObj.GetComponent<CardInstanceUI>();
        }

        cardScript.SetCardInstance(card, _user);
        cardScript._instanceId = instanceId;
        cardScript.OnRequestRemoval -= HandleCardRemove;
        cardScript.OnRequestRemoval += HandleCardRemove;
        cardScript._user = _user;

        // 每次都重新绑定，覆盖 Inspector 的静态引用
        var zone = cardScript.GetComponent<MiniCardZone>();
        if (zone != null)
            zone._instanceUI = cardScript;

        _activeCards.Add(cardScript);
        return cardScript;
    }


    public void HandleCardRemove(CardInstanceUI cardScript)
    {
        if (_activeCards.Contains(cardScript))
        {
            _activeCards.Remove(cardScript);

            // 同步清 _activeInstances
            var key = _activeInstances.FirstOrDefault(kv => kv.Value == cardScript).Key;
            if (_activeInstances.ContainsKey(key))
                _activeInstances.Remove(key);

            cardScript.gameObject.SetActive(false);
            if (!_cardPool.Contains(cardScript))
                _cardPool.Add(cardScript);
            SortLayout();
        }
    }


    private void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _activeCards.Clear();
        _cardPool.Clear();
    }
    public void SortLayout()
    {
        int count = _activeCards.Count;
        if (count == 0) return;
        float startX = -((count - 1) * (_miniCardWidth + _spacing)) / 2f;

        for (int i = 0; i < count; i++)
        {
            float targetX = startX + i * (_miniCardWidth + _spacing);
            _activeCards[i].rectTransform.DOAnchorPos(new Vector2(targetX, 0f), 0.5f);
        }
    }


}
