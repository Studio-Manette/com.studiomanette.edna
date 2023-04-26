using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomToggle : UnityEngine.UI.Toggle
{
    [HideInInspector]
    public Graphic graphic_OFF;

    protected override void Awake()
    {
        base.Awake();
        this.onValueChanged.AddListener(delegate {
            ToggleValueChanged(this);
        });
        UpdateGraphics();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateGraphics();
    }

    //Output the new state of the Toggle into Text
    void ToggleValueChanged(Toggle change)
    {
        UpdateGraphics();
    }

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateGraphics();
    }

#endif

    private void UpdateGraphics()
    {
        graphic.CrossFadeAlpha(isOn ? 1 : 0, 0, true);
        graphic_OFF.CrossFadeAlpha(isOn ? 0 : 1, 0, true);
    }
}
