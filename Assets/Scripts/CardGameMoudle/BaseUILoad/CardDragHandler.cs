using CardGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cardId; // 这张卡的唯一ID（用来告诉系统拖的是哪张牌）
    public GameObject _self;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    GameObject placeholder;
    int siblingIndex;
    private Vector3 originalScale;
    public bool _isPile = true;
    public LoadTempDeck _updateUI;
    void Awake()
    {
        _updateUI = GetComponentInParent<LoadTempDeck>();
        originalScale = transform.localScale;
        CardDragZone pile = GetComponentInParent<CardDragZone>();
        if(pile!=null)
            _isPile = pile._isPile;
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // 1. 开始拖拽
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        siblingIndex = transform.GetSiblingIndex();

        // 创建占位
        placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(originalParent);

        LayoutElement le = placeholder.AddComponent<LayoutElement>();
        LayoutElement cardLE = GetComponent<LayoutElement>();

        le.preferredWidth = cardLE.preferredWidth;
        le.preferredHeight = cardLE.preferredHeight;

        placeholder.transform.SetSiblingIndex(siblingIndex);

        transform.SetParent(transform.root, false);
        transform.position = Input.mousePosition;
        canvasGroup.blocksRaycasts = false;
        transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack);
    }

    // 2. 拖拽中
    public void OnDrag(PointerEventData eventData)
    {
        // 卡牌跟随鼠标移动
        transform.position = Input.mousePosition; 
    }

    // 3. 拖拽结束 (松开鼠标)
    public void OnEndDrag(PointerEventData eventData)
    {
        transform.DOKill();
        
        if (_isPile)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            Destroy(placeholder);
            canvasGroup.blocksRaycasts = true;
            transform.localScale = originalScale;
        }
        else
        {
            Destroy(_self);
            Destroy(placeholder);
        }
        if(_updateUI!=null)
            _updateUI.LoadTempDeckUI();
    }
}
        
    