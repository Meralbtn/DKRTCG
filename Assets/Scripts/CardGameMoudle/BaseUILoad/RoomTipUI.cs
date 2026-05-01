using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTipUI : MonoBehaviour
{
    public TMPro.TMP_InputField _inputText;

    public void FreshUI()
    {
        _inputText.text = "";
    }
   
}
