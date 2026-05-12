using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WinTipUIM : MonoBehaviour
{
    [SerializeField] private Sprite _winUI;
    [SerializeField] private Sprite _loseUI;
    [SerializeField] private Image _image;

    private void Start()
    {
        _image.gameObject.SetActive(false);

        EventCenter.AddListener(UIEvent.ON_BATTLE_WIN, OnWin);
        EventCenter.AddListener(UIEvent.ON_BATTLE_LOSE, OnLose);
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener(UIEvent.ON_BATTLE_WIN, OnWin);
        EventCenter.RemoveListener(UIEvent.ON_BATTLE_LOSE, OnLose);
    }

    private void OnWin(object _)
    {
        _image.sprite = _winUI;
        StartCoroutine(ShowTip());
    }

    private void OnLose(object _)
    {
        _image.sprite = _loseUI;
        StartCoroutine(ShowTip());
    }

    private IEnumerator ShowTip()
    {
        _image.gameObject.SetActive(true);

        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _image.color = new Color(1, 1, 1, t / 0.5f);
            yield return null;
        }
        _image.color = new Color(1, 1, 1, 1);

        yield return new WaitForSeconds(2f);

        t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _image.color = new Color(1, 1, 1, 1 - t / 0.5f);
            yield return null;
        }
        _image.color = new Color(1, 1, 1, 0);
        _image.gameObject.SetActive(false);
    }
}
