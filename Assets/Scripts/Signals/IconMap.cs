using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IconMap", menuName = "Scriptable Objects/IconMap")]
public class IconMap : ScriptableObject
{
    [System.Serializable] class IconState {public SignalIcon icon; public Sprite sprite; public Color color;}
    [SerializeField] private List<IconState> iconList;
    private Dictionary<SignalIcon, (Sprite sprite, Color color)> map; 

    public Dictionary<SignalIcon, (Sprite sprite, Color color)> Map
    {
        get {if (map == null) initMap(); return map;}
    }

    void initMap()
    {
        Debug.Log("Initializing Map");
        if (map != null) 
            return;
        map = new();
        foreach (var state in iconList)
            map.Add(state.icon, (state.sprite, state.color));
    }
}
