using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using CardGame;
using System;
public class CardInBattleUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public RectTransform rectTransform;
    public int _handIndex { get; set; }
    public Action<CardInBattleUI> OnRequestRemoval;
    public Vector3 _originalScale = new Vector3(0.5f, 0.5f, 1f); // 对应你之前的缩放
    // 放大后的比例
    public float _hoverScale = 0.65f;
    // 向上弹起的距离
    public float _moveUpAmount = 30f;
    //当卡牌被使用时，需求达到的拖拽距离
    public float _useThreshold = 200f;
    private Vector3 _dragStartPos;
    private Tween _hoverTween;
    private Vector3 _velocity = Vector3.zero;
    public float smoothTime = 0.05f;
    private Vector3 _offset;
    //卡牌的状态
    public BattleCardState _state;
    public BattleInfo _user;
    private CanvasGroup _canvasGroup;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        transform.localScale = _originalScale;
    }

    public void SetWhoUsed(BattleInfo player)
    {
        _user = player;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null) return;
        // 停止之前的动画，防止抖动
        _hoverTween?.Kill();

        // 组合动画：变大 + 向上移
        // 注意：如果用了LayoutGroup，修改localPosition会被布局组件每帧重置
        // 建议在进入时禁用 LayoutElement (如果挂了的话) 或者通过偏移实现
        transform.DOScale(_hoverScale, 0.2f).SetEase(Ease.OutBack);
        transform.DOLocalMoveY(_moveUpAmount, 0.2f).SetEase(Ease.OutCubic);

        // 提升层级，确保被放大的牌在最前面
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hoverTween?.Kill();

        transform.DOScale(_originalScale, 0.2f);
        transform.DOLocalMoveY(0, 0.2f);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _offset = transform.position - (Vector3)eventData.position;
        _hoverTween?.Kill(true);
        transform.localScale = _originalScale;
        transform.localPosition = new Vector3(transform.localPosition.x, 0, 0);
        _canvasGroup.blocksRaycasts = false;
        _dragStartPos = transform.position;
    }
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 targetPos = (Vector3)eventData.position + _offset;
        // 使用 SmoothDamp 平滑过渡
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool isUsed = false;
        if ((transform.position.y - _dragStartPos.y) > _useThreshold)
        {
            //召唤请求
            Card card = GetComponent<CardUIManager>().card;
            if (card is MinionCard)
            {
                if (card.Cost > BattleManager.Instance._playerTrueCostPoint)
                {
                    Debug.Log("费用不足");
                    //播放一个费用不足的提示动画
                    ShowInsufficientCostEffect();
                }
                // 本地预校验：不是自己的回合
                else if (!BattleManager.Instance._isMyTurn)
                {
                    Debug.Log("还没到你的回合");
                }
                // 本地预校验：场地满了（最多7个）
                else if (BattleManager.Instance._playerCardPlace.GetMiniCardCount() >= 7)
                {
                    Debug.Log("场地已满");
                }
                else
                {
                    Debug.Log("Summon Minion request");
                    BattleManager.Instance._playerTrueCostPoint -= card.Cost; // 先行扣除费用，优化用户体验
                    BattleManager.Instance.RequestPlayCard(_handIndex);
                    isUsed = true;
                }
            }
            else if (card is SpellCard)
            {
            }
        }
        if (!isUsed)
            transform.DOMove(_dragStartPos, 0.2f).SetEase(Ease.OutCubic);
        else
            ReleaseCard();

        _canvasGroup.blocksRaycasts = true; // 恢复射线阻挡
    }

    // 费用不足时的视觉反馈
    private void ShowInsufficientCostEffect()
    {
        // 抖动提示
        transform.DOShakePosition(0.3f, strength: 10f, vibrato: 20)
                 .OnComplete(() => transform.DOMove(_dragStartPos, 0.2f));
    }

    void ReleaseCard()
    {
        if (OnRequestRemoval != null)
        {
            OnRequestRemoval.Invoke(this);
        }
    }
}
