using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class urlOpener : MonoBehaviour
{
    public string url;

    public void Open()
    {
        Application.OpenURL(url);
    }

    void Update()
    {
        if (Input.GetKeyDown("f1"))
        {
            Application.OpenURL(url);
        }
    }
}
