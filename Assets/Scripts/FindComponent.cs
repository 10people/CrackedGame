using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FindComponent
{
    public static List<T> FindAllComponents<T>(GameObject go) where T : Component
    {
        return go.GetComponentsInChildren<T>(true).ToList();
    }

    public static void DisplayComponents<T>(IEnumerable<T> componentList) where T : Component
    {
        foreach (var component in componentList)
        {
            var path = AssetDatabase.GetAssetPath(component);
            Debug.Log("Find component: " + component.gameObject.name + ", path: " + path);
        }
    }
}
