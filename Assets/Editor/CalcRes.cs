using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections;

public class CalcRes : EditorWindow
{
    #region Fields

    public class PrefabItem
    {
        public string path;
        public GameObject go;
        public int animationNum;
        public int particleNum;
        public int polygenNum;
    }
    private static List<PrefabItem> PrefabItems = new List<PrefabItem>();
    private static List<string> PrefabTitles = new List<string>() { "path", "animationNum", "particleNum", "polygenNum" };

    private List<string> FindedPrefabPaths = new List<string>();
    private const string PrefabExt = ".prefab";

    private bool IsExportAnimation;
    private bool IsExportParticle;
    private bool IsExportPolygen;

    private const string SelectedFolder = "Prefabs";

    private string ExportFileName = Application.dataPath + "/ExportResDetail.xml";

    #endregion

    #region Menu Item Handler

    [MenuItem("Tool/CalcRes", false, 0)]
    public static void StartResExtract()
    {
        GetWindow<CalcRes>(false, "CalcRes", true);
    }

    #endregion

    #region Methods

    private void StartCalc()
    {
        Debug.LogWarning("Find all prefabs begins.");
        PrefabItems.Clear();
        var resultPaths = FindAllPrefabs(PrefabExt, new List<string>() { SelectedFolder });
        foreach (var path in resultPaths)
        {
            PrefabItems.Add(new PrefabItem() { path = path });
        }
        Debug.LogWarning("Find all prefabs ends.");

        Debug.LogWarning("Load all prefabs begins.");
        LoadAllPrefabs();
        Debug.LogWarning("Load all prefabs ends.");

        Debug.LogWarning("Active all prefabs begins.");
        ActiveAllPrefabs();
        Debug.LogWarning("Active all prefabs ends.");

        Debug.LogWarning("Clac all prefabs begins.");
        CalcResDetail();
        Debug.LogWarning("Clac all prefabs ends.");
    }

    private void ExportToXML()
    {
        var parsedXML = new List<List<string>>();
        foreach (var prefab in PrefabItems)
        {
            var tempList = new List<string>();
            tempList.Add(prefab.path);
            if (IsExportAnimation)
            {
                tempList.Add(prefab.animationNum.ToString());
            }
            if (IsExportParticle)
            {
                tempList.Add(prefab.particleNum.ToString());
            }
            if (IsExportPolygen)
            {
                tempList.Add(prefab.polygenNum.ToString());
            }

            parsedXML.Add(tempList);
        }

        var fileinfo = new FileInfo(ExportFileName);
        var titles = new List<string>() { "path" };
        if (IsExportAnimation)
        {
            titles.Add(PrefabTitles[1]);
        }
        if (IsExportParticle)
        {
            titles.Add(PrefabTitles[2]);
        }
        if (IsExportPolygen)
        {
            titles.Add(PrefabTitles[3]);
        }

        FileIO.WriteXML(fileinfo, parsedXML, titles);
    }

    /// <summary>
    /// find all prefabs in assets.
    /// </summary>
    /// <param name="prefabExt"></param>
    /// <param name="pathContainList">only find prefab in contain list</param>
    /// <returns>prefab path</returns>
    private IEnumerable<string> FindAllPrefabs(string prefabExt, List<string> pathContainList)
    {
        var resultPaths = new List<string>();
        var paths = AssetDatabase.GetAllAssetPaths();

        foreach (var path in paths.Where(path => path.EndsWith(prefabExt)))
        {
            var contained = pathContainList.Any(filter => path.Contains(filter));
            if (!contained)
            {
                continue;
            }
            resultPaths.Add(path);
            Debug.Log("Find prefab with path: " + path);
        }

        return resultPaths;
    }

    private void LoadAllPrefabs()
    {
        foreach (var prefab in PrefabItems)
        {
            var prefabObject = AssetDatabase.LoadAssetAtPath(prefab.path, typeof(GameObject)) as GameObject;
            if (prefabObject == null)
            {
                continue;
            }
            prefab.go = prefabObject;
        }
    }

    private void ActiveAllPrefabs()
    {
        foreach (var prefab in PrefabItems)
        {
            var prefabObject = prefab.go;
            if (prefabObject == null)
            {
                continue;
            }
            prefab.go.SetActive(true);
        }
    }

    private void CalcResDetail()
    {
        foreach (var prefab in PrefabItems)
        {
            prefab.animationNum = FindComponent.FindAllComponents<Animation>(prefab.go).Count;
            prefab.particleNum = FindComponent.FindAllComponents<ParticleSystem>(prefab.go).Count;
            var meshFilterPolygenCount = FindComponent.FindAllComponents<MeshFilter>(prefab.go).Where(item => item.sharedMesh != null).Select(item => item.sharedMesh.triangles.Length / 3).Sum();
            var skinnedMeshPolygenCount = FindComponent.FindAllComponents<SkinnedMeshRenderer>(prefab.go).Where(item => item.sharedMesh != null).Select(item => item.sharedMesh.triangles.Length / 3).Sum();
            prefab.polygenNum = meshFilterPolygenCount + skinnedMeshPolygenCount;

            Debug.Log("Prefab:" + prefab.path + ", animation num is:" + prefab.animationNum + ", particle num is:" + prefab.particleNum + ", polygen num is:" + prefab.polygenNum);
        }
    }

    #endregion

    #region Mono

    void OnGUI()
    {
        IsExportAnimation = EditorGUILayout.Toggle(
                new GUIContent
                {
                    tooltip = "Whether or not to calculate animations num",
                    text = "Export animations"
                },
                IsExportAnimation);

        IsExportParticle = EditorGUILayout.Toggle(
        new GUIContent
        {
            tooltip = "Whether or not to calculate particles num",
            text = "Export particles"
        },
        IsExportParticle);

        IsExportPolygen = EditorGUILayout.Toggle(
                new GUIContent
                {
                    tooltip = "Whether or not to calculate triSliders num",
                    text = "Export polygens"
                },
                IsExportPolygen);

        if (GUILayout.Button("StartCalc"))
        {
            StartCalc();
        }

        if (GUILayout.Button("ExportXML"))
        {
            ExportToXML();
        }
    }

    #endregion
}
