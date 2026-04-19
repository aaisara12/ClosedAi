using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SignalIcon 
{
    Circle,
    Triangle,
    Square,
    Diamond
}


[RequireComponent(typeof(SpriteRenderer))]
public class SignalStatus : MonoBehaviour
{
    [System.Serializable] class IconState {public SignalIcon icon; public Sprite sprite; public Color color;}

    [SerializeField] private List<IconState> iconList = new List<IconState>();
    private Dictionary<SignalIcon, (Sprite sprite, Color color)> iconMap;
    private SpriteRenderer _statusSprite;
    private void Start()
    {
        _statusSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void setIcon(SignalIcon s)
    {
        if (iconMap.ContainsKey(s))
        {
            _statusSprite.sprite = iconMap[s].sprite;
            _statusSprite.color = iconMap[s].color;
        }
    }
    private void resetIcon()
    {
        setIcon(SignalIcon.Circle);
    }

}