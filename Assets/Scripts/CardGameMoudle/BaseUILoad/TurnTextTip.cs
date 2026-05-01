using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class TurnTextTip : MonoBehaviour
{
    public BattleManager _battleManager;
    public TextMeshProUGUI _tipText;
    void Start()
    {
        _battleManager.ChangeBattleState = UpdateText;
    }

    private void UpdateText(string text)
    {
        _tipText.text = text;
    }
}
