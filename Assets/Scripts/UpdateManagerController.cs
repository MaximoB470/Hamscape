using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateManagerController: MonoBehaviour
{
    private int i;

    public virtual void UpdateMe()
    {
        i++;
    }
}