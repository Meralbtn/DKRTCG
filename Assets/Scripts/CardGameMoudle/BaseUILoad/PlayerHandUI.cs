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

            cardScript._serverInstanceId = instanceId;
            cardScript._handIndex = activeCards.Count;
            cardScript._user = _playerId;

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
            // 先更新数据再激活，避免旧数值闪烁一帧
            cardScript.GetComponent<CardUIManager>().card = drawCards;
            cardScript.GetComponent<CardUIManager>().InitialCard();
            cardScript.transform.SetParent(this.transform);
            cardScript.gameObject.SetActive(true);
        }
        else
        {
            GameObject cardObj = Instantiate(_cardPrefab, transform, false);
            cardScript = cardObj.GetComponent<CardInBattleUI>();
            cardObj.GetComponent<CardUIManager>().state = CardState.Battle;
            cardObj.GetComponent<CardUIManager>().card = drawCards;
            cardObj.GetComponent<CardUIManager>().InitialCard();
        }

        cardScript._serverInstanceId = instanceId;
        cardScript._handIndex = activeCards.Count;
        cardScript._user = _playerId;
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
        var serverIds = new HashSet<int>(serverHand.Select(h => h.InstanceId));
        Debug.Log($"SyncFromServer: 服务器手牌数={serverHand.Count} serverIds={string.Join(",", serverIds)}");
        Debug.Log($"SyncFromServer: 本地手牌数={activeCards.Count} localIds={string.Join(",", activeCards.Select(c => c._serverInstanceId))}");

        var toRemove = activeCards
            .Where(c => !serverIds.Contains(c._serverInstanceId))
            .ToList();

        Debug.Log($"SyncFromServer: 需要移除={toRemove.Count}");
        foreach (var card in toRemove)
            HandleCardRemove(card);

        var localIds = new HashSet<int>(activeCards.Select(c => c._serverInstanceId));
        var toAdd = serverHand.Where(h => !localIds.Contains(h.InstanceId)).ToList();
        Debug.Log($"SyncFromServer: 需要新增={toAdd.Count}");

        foreach (var handCard in toAdd)
        {
            Card cardData = PlayerManager.Instance().GetCardList()[handCard.CardId];
            AddDrawCard(cardData, handCard.InstanceId);
        }

        for (int i = 0; i < activeCards.Count; i++)
            activeCards[i]._handIndex = i;

        SortLayout();
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
            int removeCount = current - enemyHandCount;
            for (int i = 0; i < removeCount; i++)
            {
                var last = activeCards[activeCards.Count - 1];
                HandleCardRemove(last);
            }
        }
        // 数量相同不操作，不闪烁
    }  

    public void RefreshOutlineByAffordability(int currentCost)
    {
        bool fieldFull = BattleManager.Instance != null &&
                         BattleManager.Instance._playerCardPlace.GetMiniCardCount() >= 5;
        foreach (Transform child in transform)
        {
            var cardView  = child.GetComponent<CardUIManager>();
            var cardView1 = child.GetComponent<CardInBattleUI>();
            if (cardView == null) continue;

            bool canAfford = cardView.card.Cost <= currentCost;
            bool blocked   = cardView.card is MinionCard && fieldFull;
            var state = (canAfford && !blocked) ? OutlineState.Available : OutlineState.None;
            cardView1.OutlineController.SetOutline(state);
        }
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
