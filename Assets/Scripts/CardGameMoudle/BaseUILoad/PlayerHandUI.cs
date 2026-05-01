using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;
using UnityEngine.PlayerLoop;
using CardGameClient;
using DG.Tweening;
using System.Linq;
public class PlayerHandUI : MonoBehaviour
{
    #region TestValue
    public BattleInfo _playerId;
    public GameObject _cardPrefab;
    //缩放后的实际宽度
    public float cardWidth = 125f;
    //卡牌间隔
    public float spacing = 10f;
    #endregion
    private RectTransform _containerRt;

    //对象池优化
    private List<CardInBattleUI> cardPool = new List<CardInBattleUI>();
    public List<CardInBattleUI> activeCards = new List<CardInBattleUI>();

    void Start()
    {
        _containerRt = GetComponent<RectTransform>();
    }

    public void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        activeCards.Clear();
        cardPool.Clear();
    }

    public void AddDrawCard(Card drawCards, int instanceId = -1)
    {
        CardInBattleUI cardScript;

        if (drawCards == null)
        {
            // 敌方卡背
            if (cardPool.Count > 0)
            {
                cardScript = cardPool[0];
                cardPool.RemoveAt(0);
                cardScript.gameObject.SetActive(true);
                cardScript.transform.SetParent(this.transform);
            }
            else
            {
                GameObject cardObj = Instantiate(_cardPrefab, transform, false);
                cardScript = cardObj.GetComponent<CardInBattleUI>();
                cardObj.GetComponent<CardUIManager>().state = CardState.Battle;
                cardObj.GetComponent<CardUIManager>().InitialBackCard();
            }

            cardScript._handIndex = instanceId;
            cardScript._user         = _playerId;
            // 禁用敌方卡牌交互
            var cg = cardScript.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;

            activeCards.Add(cardScript);
            SortLayout();
            return;
        }

        // 我方手牌
        if (cardPool.Count > 0)
        {
            cardScript = cardPool[0];
            cardPool.RemoveAt(0);
            cardScript.gameObject.SetActive(true);
            cardScript.transform.SetParent(this.transform);
            cardScript.GetComponent<CardUIManager>().card = drawCards;
            cardScript.GetComponent<CardUIManager>().InitialCard();
        }
        else
        {
            GameObject cardObj = Instantiate(_cardPrefab, transform, false);
            cardScript = cardObj.GetComponent<CardInBattleUI>();
            cardObj.GetComponent<CardUIManager>().state = CardState.Battle;
            cardObj.GetComponent<CardUIManager>().card  = drawCards;
            cardObj.GetComponent<CardUIManager>().InitialCard();
        }

        cardScript._handIndex = instanceId;  // 记录 HandId
        cardScript._user         = _playerId;
        cardScript.OnRequestRemoval -= HandleCardRemove;
        cardScript.OnRequestRemoval += HandleCardRemove;

        activeCards.Add(cardScript);
        SortLayout();
    }

    private void HandleCardRemove(CardInBattleUI cardScript)
    {
        if (activeCards.Contains(cardScript))
        {
            activeCards.Remove(cardScript);

            cardScript.gameObject.SetActive(false);

            if (!cardPool.Contains(cardScript))
            {
                cardPool.Add(cardScript);
            }
            SortLayout();
        }
    }

    public void SyncFromServer(List<HandCardState> serverHand)
    {
       //找出需要删除的牌
        var serverIds = new HashSet<int>(serverHand.Select(h => h.InstanceId));
        var toRemove = activeCards
            .Where(c => !serverIds.Contains(c._handIndex))
            .ToList();

        foreach (var card in toRemove)
            HandleCardRemove(card);

        // 2. 找出需要新增的牌（服务器有，本地没有）
        var localIds = new HashSet<int>(activeCards.Select(c => c._handIndex));
        var toAdd = serverHand.Where(h => !localIds.Contains(h.InstanceId)).ToList();

        foreach (var handCard in toAdd)
        {
            Card cardData = PlayerManager.Instance().GetCardList()[handCard.CardId];
            AddDrawCard(cardData, handCard.InstanceId);
        }
    }

    public void SyncEnemyFromServer(int enemyHandCount)
    {
        int current = activeCards.Count;

        if (enemyHandCount > current)
        {
            // 新增卡背
            for (int i = 0; i < enemyHandCount - current; i++)
                AddDrawCard(null, -1);
        }
        else if (enemyHandCount < current)
        {
            // 从末尾移除多余卡背
            for (int i = current - 1; i >= enemyHandCount; i--)
                HandleCardRemove(activeCards[i]);
        }
        // 数量相同不操作，不闪烁
    }

    //布局调整
    public void SortLayout()
    {
        int count = activeCards.Count;
        if (count == 0) return;
        float startX = -((count - 1) * (cardWidth + spacing)) / 2f;

        for (int i = 0; i < count; i++)
        {
            float targetX = startX + i * (cardWidth + spacing);
            activeCards[i].rectTransform.DOAnchorPos(new Vector2(targetX, 0), 0.3f).SetEase(Ease.OutQuad);
        }
    }
}
