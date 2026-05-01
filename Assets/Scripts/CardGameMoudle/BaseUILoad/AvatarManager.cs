using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance;

    public Dictionary<string, Sprite> avatarCache = new Dictionary<string, Sprite>();

    public Image AvatarImage;
    
    private void Awake()
    {
        AvatarImage = GetComponentInChildren<Image>();
        Instance = this;
    }

    public void SetAvatar(string avatarId)
    {
        AvatarImage.sprite = avatarCache[avatarId];
    }
}
