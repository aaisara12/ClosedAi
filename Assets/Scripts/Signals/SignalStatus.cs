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
    // [System.Serializable] class IconState {public SignalIcon icon; public Sprite sprite; public Color color;}

    // [SerializeField] private List<IconState> iconList = new List<IconState>();
    // private Dictionary<SignalIcon, (Sprite sprite, Color color)> iconMap;
    private SpriteRenderer _statusSprite;
    private void Start()
    {
        _statusSprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetIcon(SignalIcon s)
    {
        if (iconMap.map.ContainsKey(s))
        {
            _statusSprite.sprite = iconMap.map[s].sprite;
            _statusSprite.color = iconMap.map[s].color;
        }
    }
    public void ResetIcon()
    {
        SetIcon(SignalIcon.Circle);
    }

}