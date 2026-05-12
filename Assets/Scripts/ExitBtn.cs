using System.Collections;
using System.Collections.Generic;
using CardGameClient;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    public void GoToMainMenu()
    {
        _ = CardClient.Instance.SendSurrender();
        SceneManager.LoadScene("MainMenu");
    }
}
