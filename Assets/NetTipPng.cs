using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;
using UnityEngine.UI;
public class NetTipPng : MonoBehaviour
{
    [SerializeField] private Sprite _netOff;
    [SerializeField] private Sprite _netOnline;
    [SerializeField] private Image _image;

    void Start()
    {
        // 监听登录/登出事件
        EventCenter.AddListener(UIEvent.ON_LOGIN_SUCCESS, OnLoginStateChanged);
        EventCenter.AddListener(UIEvent.ON_LOGIN_FAILED, OnLoginStateChanged);
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener(UIEvent.ON_LOGIN_SUCCESS, OnLoginStateChanged);
        EventCenter.RemoveListener(UIEvent.ON_LOGIN_FAILED, OnLoginStateChanged);
    }

    void Update()
    {
        bool isLoggedIn = PlayerManager.Instance().IsLogin();
        _image.sprite = isLoggedIn ? _netOnline : _netOff;
    }

    private void OnLoginStateChanged(object _)
    {
        bool isLoggedIn = PlayerManager.Instance().IsLogin();
        _image.sprite = isLoggedIn ? _netOnline : _netOff;
    }
}
