using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IconMap", menuName = "Scriptable Objects/IconMap")]
public class IconMap : ScriptableObject
{
    [System.Serializable] class IconState {public SignalIcon icon; public Sprite sprite; public Color color;}
    [SerializeField] private List<IconState> iconList = new List<IconState>();
    public Dictionary<SignalIcon, (Sprite sprite, Color color)> map = new Dictionary<SignalIcon, (Sprite sprite, Color color)>();
    void onEnable()
    {
        Debug.Log("Initializing Dict");
        foreach (var state in iconList)
            map.Add(state.icon, (state.sprite, state.color));
    }
}
