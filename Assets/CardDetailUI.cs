using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame;
using System.Collections;

public class CardDetailPanel : MonoBehaviour
{
    public static CardDetailPanel Instance;

    [Header("根节点")]
    [SerializeField] private GameObject _cardInfo;

    [Header("名字")]
    [SerializeField] private TextMeshProUGUI _nameText;

    [Header("卡牌类型")]
    [SerializeField] private TextMeshProUGUI _typeText;

    [Header("cost")]
    [SerializeField] private TextMeshProUGUI _costText;

    [Header("描述区")]
    [SerializeField] private TextMeshProUGUI _desText;

    [Header("战斗属性")]
    [SerializeField] private TextMeshProUGUI _heText;
    [SerializeField] private TextMeshProUGUI _attackText;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _cardInfo.SetActive(false);
    }

    public void Show(Card card)
    {
     

        _nameText.text = card.CardName;
        _costText.text = "Cost - " + card.Cost.ToString();

        if (card is MinionCard minion)
        {
            _heText.text = minion.Health.ToString();
            _attackText.text = minion.Attack.ToString();
            _typeText.text = "Minion";
            _desText.text = string.IsNullOrEmpty(card.Description) ? "无描述" : card.Description;
        }
        else if (card is SpellCard spell)
        {
            _typeText.text = "Spell";
            _desText.text = string.IsNullOrEmpty(card.Description) ? "无描述" : card.Description;
        }

        _cardInfo.SetActive(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(AutoHide());
    }

    public void Hide()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        _cardInfo.SetActive(false);
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(2f);
        _cardInfo.SetActive(false);
        _hideCoroutine = null;
    }
}