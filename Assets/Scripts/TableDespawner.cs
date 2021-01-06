using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Written by Steven Lay, 2020.
 * Removes the Table outside Unity Editor.
 * Only seen in Unity play mode for visualisation purposes.
 */
public class TableDespawner : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }
}
