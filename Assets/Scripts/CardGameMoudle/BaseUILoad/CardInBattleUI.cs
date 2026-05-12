using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using CardGame;
using System;

public class CardInBattleUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform rectTransform;
    public int _handIndex { get; set; }
    public int _serverInstanceId { get; set; } = -1;
    public Action<CardInBattleUI> OnRequestRemoval;

    public Vector3 _originalScale = new Vector3(0.5f, 0.5f, 1f);
    public float _hoverScale = 0.65f;
    public float _moveUpAmount = 30f;
    public float _useThreshold = 200f;   // 屏幕像素，拖拽触发出牌的最小 Y 距离

    private Vector2 _dragStartAnchoredPos;
    private float _dragScreenDeltaY;     // 累计屏幕像素 Y，用于出牌阈值判断
    private Sequence _hoverSeq;
    private bool _isAnimating;
    private Canvas _rootCanvas;

    public BattleCardState _state;
    public BattleInfo _user;
    private CanvasGroup _canvasGroup;
    private CardUIManager _cardUIManager;
    private CardDissolveEffect _dissolveEffect;
    public CardOutlineController OutlineController { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _cardUIManager = GetComponent<CardUIManager>();
        _dissolveEffect = GetComponent<CardDissolveEffect>();
        OutlineController = GetComponent<CardOutlineController>();
        _rootCanvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        // 仅在战斗模式启用时才强制 scale，卡组视图不受影响
        transform.localScale = _originalScale;
    }

    public void SetWhoUsed(BattleInfo player) => _user = player;

    // ── Hover ───────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isAnimating || eventData.pointerDrag != null) return;
        _hoverSeq?.Kill();
        _hoverSeq = DOTween.Sequence()
            .Join(transform.DOScale(_hoverScale, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOLocalMoveY(_moveUpAmount, 0.2f).SetEase(Ease.OutCubic));
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isAnimating) return;
        _hoverSeq?.Kill();
        _hoverSeq = DOTween.Sequence()
            .Join(transform.DOScale(_originalScale, 0.2f))
            .Join(transform.DOLocalMoveY(0f, 0.2f));
    }

    // ── Drag ────────────────────────────────────────────────

    // 禁用 EventSystem 内置拖拽阈值，让拖拽立即响应，消除起步卡顿
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isAnimating) return;

        _hoverSeq?.Kill();
        transform.DOKill();

        transform.localScale = _originalScale;
        transform.localPosition = new Vector3(transform.localPosition.x, 0f, 0f);

        _dragStartAnchoredPos = rectTransform.anchoredPosition;
        _dragScreenDeltaY = 0f;

        _canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isAnimating) return;

        // delta 是屏幕像素，除以 scaleFactor 转为 Canvas 本地单位
        float scale = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scale;
        _dragScreenDeltaY += eventData.delta.y;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        transform.DOKill();

        if (_isAnimating) return;

        if (_dragScreenDeltaY <= _useThreshold)
        {
            ReturnToHand();
            return;
        }

        Card card = _cardUIManager.card;

        // ── 前置校验，失败直接回手 ────────────────────────
        if (!BattleManager.Instance._isMyTurn)
        {
            ReturnToHand();
            return;
        }
        if (card.Cost > BattleManager.Instance._playerTrueCostPoint)
        {
            ShowInsufficientCostEffect();
            return;
        }
        if (card is MinionCard && BattleManager.Instance._playerCardPlace.GetMiniCardCount() >= 5)
        {
            ShowInsufficientCostEffect();
            return;
        }

        // ── 出牌成功 ──────────────────────────────────────
        _isAnimating = true;
        BattleManager.Instance._playerTrueCostPoint -= card.Cost;

        if (card is MinionCard || (card is SpellCard sp && !sp.NeedsTarget))
        {
            PlayCardAnimation(() =>
            {
                _isAnimating = false;
                ReleaseCard();
                BattleManager.Instance.RequestPlayCard(_handIndex);
            });
        }
        else if (card is SpellCard spellCard && spellCard.NeedsTarget)
        {
            _isAnimating = false;
        }
    }

    // ── 辅助 ────────────────────────────────────────────────

    private void ReturnToHand()
    {
        rectTransform.DOAnchorPos(_dragStartAnchoredPos, 0.25f).SetEase(Ease.OutCubic);
    }

    private void ShowInsufficientCostEffect()
    {
        _isAnimating = true;
        rectTransform.DOShakeAnchorPos(0.3f, strength: 10f, vibrato: 20)
            .OnComplete(() =>
            {
                _isAnimating = false;
                rectTransform.DOAnchorPos(_dragStartAnchoredPos, 0.2f).SetEase(Ease.OutCubic);
            });
    }

    private void PlayCardAnimation(Action onComplete)
    {
        if (_dissolveEffect != null)
            _dissolveEffect.PlayDissolve(onComplete);
        else
            onComplete?.Invoke();
    }

    private void ReleaseCard()
    {
        OnRequestRemoval?.Invoke(this);
    }
}
