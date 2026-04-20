using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SignalIcon 
{
    Circle,
    Triangle,
    Square,
    Diamond,
    RedCircle
}


// See IconMap.cs for global icon defs

[RequireComponent(typeof(SpriteRenderer))]
public class SignalStatus : MonoBehaviour
{
    [SerializeField] IconMap iconMap;
    private SpriteRenderer _statusSprite;
    private void Start()
    {
        _statusSprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetIcon(SignalIcon s)
    {
        Debug.Log("Setting Icon");
        Debug.Log(iconMap.Map.Keys.Count);
        if (iconMap.Map.ContainsKey(s))
        {
            _statusSprite.sprite = iconMap.Map[s].sprite;
            _statusSprite.color = iconMap.Map[s].color;
        }
    }
    public void ResetIcon()
    {
        SetIcon(SignalIcon.Circle);
    }

}