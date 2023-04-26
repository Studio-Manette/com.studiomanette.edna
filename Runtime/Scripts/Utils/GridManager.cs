using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject grid;


    public void onClick()
    {
        if (grid.activeInHierarchy == true)
            grid.SetActive(false);
        else
            grid.SetActive(true);
    }
}
