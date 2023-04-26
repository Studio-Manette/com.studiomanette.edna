using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateManager : MonoBehaviour
{
    public GameObject[] ObjectsToEnableOnCalibrate;

    public void Awake()
    {
        OnClick(false);
    }

    public void OnClick(bool _isActive)
    {
        foreach (GameObject go in ObjectsToEnableOnCalibrate)
        {
            go.SetActive(_isActive);
        }
    }
}
