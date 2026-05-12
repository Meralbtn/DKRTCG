using UnityEngine;
using CardGameClient;

public class AutoLoginHandler : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private string _debugEmail;
    [SerializeField] private string _debugPassword;
#endif

    private async void Start()
    {
        // 等待 TCP 连接和 HelloServer 握手完成再发送登录包
        if (CardClient.Instance._connectTask != null)
            await CardClient.Instance._connectTask;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(_debugEmail) && !string.IsNullOrEmpty(_debugPassword))
        {
            await CardClient.Instance.SendLogin(_debugEmail, _debugPassword);
            return;
        }
#endif
        // 发布版：用上次登录成功时存下的凭证自动登录
        string savedEmail = PlayerPrefs.GetString("AutoLogin_Email", "");
        string savedHash  = PlayerPrefs.GetString("AutoLogin_Hash",  "");
        if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedHash))
            await CardClient.Instance.SendLoginWithHash(savedEmail, savedHash);
    }
}
