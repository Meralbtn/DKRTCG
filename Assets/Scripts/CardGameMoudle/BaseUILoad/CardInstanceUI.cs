using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CardGame;
namespace CardGame
{
    public class CardInstance
    {
        //怪兽实体，场地实体,可以被挂载武器，增加buff
        private Card _card;
        private CardType _cardType;
        public int _instanceHealth { get; set; }
        public int _instanceAttack { get; set; }
        private bool _isInitialized = false;
        private int _attackCount;
        private int _trueAttack;
        //武器后续更新
        public CardInstance(Card card)
        {
            this._card = card;
            Initialize();
        }
        private void Initialize()
        {
            if (_isInitialized) return;
            if (_card is MinionCard minionCard)
            {
                _instanceHealth = minionCard.Health;
                _instanceAttack = minionCard.Attack;
                _cardType = CardType.Minion;
                _attackCount = 1;
            }
            _isInitialized = true;
        }

        public void HealthD(int value)
        {
            _instanceHealth -= value;
        }
        public bool IsDead()
        {
            if (_instanceHealth <= 0)
                return true;

            return false;
        }

        public Card GetCard()
        {
            return _card;
        }
        public CardType GetCardType()
        {
            return _cardType;
        }

        public void AddAttackValue(int value)
        {
            _instanceAttack += value;
        }

        public int GetAttackValue()
        {
            return _instanceAttack;
        }

        public int GetAttackCount()
        {
            return _trueAttack;
        }


        public void UpdateAttack()
        {
            _trueAttack = _attackCount;
        }

        public void Attack()
        {
            _trueAttack--;
        }

    }
}

public class CardInstanceUI : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    public Action<CardInstanceUI> OnRequestRemoval;
    public RectTransform rectTransform;

    private bool _isActive = false;
    public BattleInfo _user;
    public TextMeshProUGUI _healthText;
    public TextMeshProUGUI _attackText;
    public Image _cardArtImage;
    private CardInstance _cardInstance;
    public GameObject _arrowPrefab;
    private GameObject _arrow;
    public int _instanceId;
    private int _attackUsed;
    private CardFrameApplier _frameApplier;

    private static readonly Color ColorCanAttack    = new Color(0.15f, 1f,   0.25f, 1f);
    private static readonly Color ColorCannotAttack = new Color(1f,   0.2f,  0.15f, 1f);
    private static readonly Color ColorDefault      = new Color(0f,   0.9f,  1f,    1f);

    private void Awake()
    {
        rectTransform  = GetComponent<RectTransform>();
        _frameApplier  = GetComponentInChildren<CardFrameApplier>();
    }
    public void DestroySelf()
    {
        if (OnRequestRemoval != null)
        {
            OnRequestRemoval.Invoke(this);
        }
    }
    public void SetCardInstance(Card card, BattleInfo user)
    {
        _cardInstance = new CardInstance(card);
        _healthText.text = _cardInstance._instanceHealth.ToString();
        _attackText.text = _cardInstance._instanceAttack.ToString();
        _cardInstance.UpdateAttack();
        _user = user;
        LoadMiniCardImage(card.CardID);
    }

    private void LoadMiniCardImage(int cardId)
    {
        if (_cardArtImage == null) return;
        Sprite sprite = Resources.Load<Sprite>($"CardImage/MiniCard/{cardId}");
        if (sprite != null)
            _cardArtImage.sprite = sprite;
        else
            Debug.LogWarning($"找不到 mini 卡图: CardImage/MiniCard/{cardId}");
    }
    public CardInstance GetCardInstance()
    {
        return _cardInstance;
    }

    public void UpdateStats(int hp, int attack, int attackUsed)
    {
        // 更新本地 CardInstance 数值
        if (_cardInstance != null)
        {
            _cardInstance._instanceHealth = hp;
            _cardInstance._instanceAttack = attack;
        }

        // 更新 UI 显示
        _healthText.text = hp.ToString();
        _attackText.text = attack.ToString();

        _attackUsed = attackUsed;
        RefreshAttackVisual(attackUsed);
    }

    public int GetAttackUsed() => _attackUsed;

    public void RefreshAttackVisual(int attackUsed)
    {
        bool isMyMinion = BattleManager.Instance != null &&
                          _user == BattleManager.Instance._playerIsFirst;


        if (_frameApplier == null)
        {
            Debug.LogWarning($"[CardInstanceUI] frameApplier 为空，id={_instanceId}，请检查 Outline 子对象是否挂了 CardFrameApplier");
            return;
        }

        if (!isMyMinion)
        {
            _frameApplier.BorderColor = ColorDefault;
            return;
        }

        bool canAttack = attackUsed == 0 && BattleManager.Instance._isMyTurn;
        Debug.Log($"[CardInstanceUI] id={_instanceId} attackUsed={attackUsed} isMyTurn={BattleManager.Instance._isMyTurn} → {(canAttack ? "绿" : "红")}");
        _frameApplier.BorderColor = canAttack ? ColorCanAttack : ColorCannotAttack;
    }

    //攻击指向箭头
    //应该将脚本分离
    public void OnBeginDrag(PointerEventData eventData)
    {
        _arrow = Instantiate(_arrowPrefab, transform.root);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
       transform.root as RectTransform,
       RectTransformUtility.WorldToScreenPoint(null, transform.position),
       null,
       out Vector2 cardCenterInCanvas);
        _arrow.GetComponent<ArrowUI>().Begin(cardCenterInCanvas);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
        transform.root as RectTransform,
        eventData.position,
        null,
        out Vector2 localPoint);
        //实现光线逻辑，和攻击逻辑
        _arrow.GetComponent<ArrowUI>().UpdateEnd(localPoint);

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _arrow.GetComponent<ArrowUI>().End();
        Debug.Log("test");
        //获取脚本
        MiniCardZone enemy = null;
        if (eventData.pointerEnter != null && _cardInstance.GetAttackCount() > 0)
        {
            Debug.Log("射线击中了: " + eventData.pointerEnter.name);
            Debug.Log("pointerEnter");
            enemy = eventData.pointerEnter.GetComponent<MiniCardZone>();
        }
        if (enemy == null)
        {
            if (BattleManager.Instance._enemyCardPlace.GetMiniCardCount() > 0)
            {
                Debug.Log("敌方有随从，不能直攻英雄");
                return;
            }
            BattleManager.Instance.RequestAttack(_instanceId, -1);
        }
        else
        {
            Debug.Log("AttackRequest2");
            BattleManager.Instance.RequestAttack(_instanceId, enemy._instanceUI._instanceId);
        }
    }


}
