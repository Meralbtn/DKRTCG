using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CardGame;
using CardGameClient;
public class MenuButtunClick : MonoBehaviour
{
    public GameObject _LoginPanel;
    public GameObject _RegisterPanel;
    //login
    public PixelInputField _emailInput;
    public PixelInputField _passwordInput;

    //register
    public PixelInputField _REemailInput;
    public PixelInputField _REpasswordInput;
    public PixelInputField _REuserNameInput;

    private void OnEnable()
    {
        EventCenter.AddListener(UIEvent.ON_LOGIN_SUCCESS, OnLoginSuccess);
        EventCenter.AddListener(UIEvent.ON_LOGIN_FAILED, OnLoginFailed);
        EventCenter.AddListener(UIEvent.ON_REGISTER_SUCCESS, OnRegisterSuccess);
        EventCenter.AddListener(UIEvent.ON_REGISTER_FAILED, OnRegisterFailed);
    }
    private void OnDisable()
    {
        EventCenter.RemoveListener(UIEvent.ON_LOGIN_SUCCESS, OnLoginSuccess);
        EventCenter.RemoveListener(UIEvent.ON_LOGIN_FAILED, OnLoginFailed);
        EventCenter.RemoveListener(UIEvent.ON_REGISTER_SUCCESS, OnRegisterSuccess);
        EventCenter.RemoveListener(UIEvent.ON_REGISTER_FAILED, OnRegisterFailed);
    }
    public void OnMenuLoginButtonClick()
    {
        _LoginPanel.SetActive(true);
        _RegisterPanel.SetActive(false);
    }
    private void SetTip(string msg)
    {
        // 这里可以使用一个专门的UI元素来显示提示信息，比如一个TextMeshProUGUI组件
        Debug.Log(msg);
    }

    public void OnLogInButtonClick()
    {
        string email = _emailInput._input.text;
        string password = _passwordInput._input.text;
        if (_emailInput ==null || _passwordInput == null)
        {
            SetTip("邮箱或密码为初始化");
            return;
        }
        Debug.Log($"Attempting login with Email: {email}, Password: {password}");
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetTip("邮箱或密码不能为空");
            return;
        }

        SetTip("登录中...");
        _ = CardClient.Instance.SendLogin(email, password);
    }
    public void OnMenuRegisterButtonClick()
    {
        _LoginPanel.SetActive(false);
        _RegisterPanel.SetActive(true);
    }
    public void OnRegisterButtonClick()
    {
        string userName = _REuserNameInput._input.text;
        string email    = _REemailInput._input.text;
        string password = _REpasswordInput._input.text;

        Debug.Log($"Attempting registration with UserName: {userName}, Email: {email}, Password: {password}");
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) 
            || string.IsNullOrEmpty(password))
        {
            SetTip("请填写完整信息");
            return;
        }

        if (password.Length < 6)
        {
            SetTip("密码至少6位");
            return;
        }

        SetTip("注册中...");
        _ = CardClient.Instance.SendRegister(userName, email, password);
    }

    private void OnLoginSuccess(object obj)
    {
        var result = obj as AuthResultPackage;
        SetTip("登录成功！");
        // 跳转大厅场景
        _LoginPanel.SetActive(false);
        _RegisterPanel.SetActive(false);
    }

    private void OnLoginFailed(object obj)
    {
        var result = obj as AuthResultPackage;
        SetTip(result?.Tips ?? "登录失败");
    }

    private void OnRegisterSuccess(object obj)
    {
        SetTip("注册成功，请登录");
        // 自动切换到登录面板
        _LoginPanel.SetActive(true);
        _RegisterPanel.SetActive(false);
        // 把注册的邮箱填到登录框里方便用户
        _emailInput._input.text = _REemailInput._input.text;
    }

    private void OnRegisterFailed(object obj)
    {
        var result = obj as AuthResultPackage;
        SetTip(result?.Tips ?? "注册失败");
    }
    public void OnExitButtonClick()
    {
        _LoginPanel.SetActive(false);
        _RegisterPanel.SetActive(false);
    }

    public void OnExitApp()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}
