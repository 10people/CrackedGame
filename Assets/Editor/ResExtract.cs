using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

public class ResExtract : EditorWindow
{
    #region Private Fileds

    private bool run;
    private static IEnumerator en;
    private Texture2D[] texs;
    private AssetBundle bundle;
    private string basePath;
    private Object textureFolder;
    private Object rightMeshUvFolder;
    private string rightMeshUvPath;
    private Object resObj;
    private bool createAnimSubFolder;
    private bool createMeshSubFolder;
    private bool createMatSubFolder; 
    private bool needAdjustUv;
    private Transform curTran;
    private Object[] allObjs;
    private readonly Dictionary<MeshFilter, string> filterMap = new Dictionary<MeshFilter, string>();
    private readonly Dictionary<SkinnedMeshRenderer, string> skinnedMap = new Dictionary<SkinnedMeshRenderer, string>();
    private readonly Dictionary<SkinnedMeshRenderer, List<string>> skinnedForMatMap = new Dictionary<SkinnedMeshRenderer, List<string>>();
    private readonly Dictionary<MeshRenderer, List<string>> renderMap = new Dictionary<MeshRenderer, List<string>>();
    private readonly Dictionary<TrailRenderer, List<string>> trailMap = new Dictionary<TrailRenderer, List<string>>();
    private readonly Dictionary<ParticleSystem, string> psMap = new Dictionary<ParticleSystem, string>();
    private readonly Dictionary<Animation, List<string>> animMap = new Dictionary<Animation, List<string>>();

    #endregion

    #region Menu Item Handler

    [MenuItem("Tool/ResExtract", false, 0)]
    public static void StartResExtract()
    {
        GetWindow<ResExtract>(false, "ResExtract", true);
    }

    #endregion

    #region Unity Call Back Functions

    private void OnGUI()
    {
        textureFolder = EditorGUILayout.ObjectField("Texture Folder", textureFolder, typeof(object), false);
        resObj = EditorGUILayout.ObjectField("Asset File", resObj, typeof(object), false);
        CheckCreatingSubFolders();
        needAdjustUv = EditorGUILayout.Toggle("Need Adjust UV", needAdjustUv);
        if (needAdjustUv)
        {
            rightMeshUvFolder = EditorGUILayout.ObjectField("Currect UV Mesh Folder", rightMeshUvFolder, typeof(object), false);
            if (rightMeshUvFolder != null)
            {
                rightMeshUvPath = Application.dataPath + "/" +
                                  AssetDatabase.GetAssetPath(rightMeshUvFolder).Substring(7) + "/";
            }
        }
        if (GUILayout.Button("Select Save Path"))
        {
            basePath = EditorUtility.SaveFolderPanel(
                "Select the Extract Folder(Must related to Assets Folder)",
                "",
                "");
            var index = basePath.IndexOf("Assets/");
            basePath = basePath.Substring(index);
            basePath += "/";
        }
        GUI.enabled = !string.IsNullOrEmpty(basePath);
        if (GUILayout.Button("StartExtract"))
        {
            CheckFolders();
            if (bundle)
            {
                bundle.Unload(true);
            }
            if (textureFolder != null)
            {
                texs = Utils.GetAtPath<Texture2D>(AssetDatabase.GetAssetPath(textureFolder).Substring(7));
            }
            run = true;
        }
        GUI.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!run)
        {
            if (en != null)
                en = null;
            return;
        }
        if (en == null)
        {
            var path = "file://" + Application.dataPath + "/" + AssetDatabase.GetAssetPath(resObj).Substring(7);
            en = LoadAsset(path);
        }
        if (!en.MoveNext())
            run = false;
    }

    #endregion

    #region Other Private Methods

    private IEnumerator LoadAsset(string assetPath)
    {
        Debug.Log("Loading " + assetPath + " ...");
        var request = new WWW(assetPath);
        while (!request.isDone)
        {
            yield return "";
        }
        if (request.error != null)
        {
            Debug.LogError(request.error);
        }
        run = false;
        en = null;
        bundle = request.assetBundle;
        allObjs = bundle.LoadAllAssets(typeof(Transform)).ToArray();
        foreach (var o in allObjs)
        {
            var obj = Instantiate(o);
            ExtractScenePrefab(obj);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ExtractScenePrefab(Object obj)
    {
        curTran = obj as Transform;
        if (curTran != null)
        {
            InitVaribles();
            CreateRaws();
            AttachReferences();
            var prefab = PrefabUtility.CreateEmptyPrefab(basePath + "Prefabs/" + obj.name + ".prefab");
            PrefabUtility.ReplacePrefab(curTran.gameObject, prefab);
            CleanData();
        }
    }

    private void CreateRaws()
    {
        CreatMeshes();
        CreateMats();
        CreateAnims();
    }

    private void AttachReferences()
    {
        AttachMeshes();
        AttachMats();
        AttachAnims();
    }

    private void CleanData()
    {
        filterMap.Clear();
        skinnedMap.Clear();
        renderMap.Clear();
        psMap.Clear();
        animMap.Clear();
    }

    private void CheckFolders()
    {
        CheckFolder("Animations");
        CheckFolder("Meshes");
        CheckFolder("Materials");
        CheckFolder("Prefabs");
    }

    private void CheckFolder(string folderName)
    {
        if (AssetDatabase.LoadAssetAtPath(basePath + folderName, typeof(object)) == null)
        {
            AssetDatabase.CreateFolder(basePath.Substring(0, basePath.Length - 1), folderName);
        }
    }

    private void InitVaribles()
    {
        Debug.Log("Starting Init Varibles.....");
        var filters = curTran.GetComponentsInChildren<MeshFilter>(true);
        foreach (var filter in filters)
        {
            var sharedMesh = filter.sharedMesh;
            if (!sharedMesh)
            {
                continue;
            }
            filterMap.Add(filter, sharedMesh.name);
        }

        var renders = curTran.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var render in renders)
        {
            var sharedMaterials = render.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                continue;
            }
            renderMap.Add(render, sharedMaterials.Where(mat => mat != null).Select(mat => mat.name).ToList());
        }

        var trails = curTran.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var tRender in trails)
        {
            var sharedMaterials = tRender.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                continue;
            }
            trailMap.Add(tRender, sharedMaterials.Where(mat => mat != null).Select(mat => mat.name).ToList());
        }

        var skinneds = curTran.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skin in skinneds)
        {
            var sharedMesh = skin.sharedMesh;
            if (!sharedMesh)
            {
                continue;
            }
            skinnedMap.Add(skin, sharedMesh.name);
            var sharedMats = skin.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0)
            {
                continue;
            }
            skinnedForMatMap.Add(skin, sharedMats.Where(mat => mat != null).Select(mat => mat.name).ToList());
        }

        var pss = curTran.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particleSystem in pss)
        {
            if (particleSystem.GetComponent<Renderer>() && particleSystem.GetComponent<Renderer>().sharedMaterial)
            {
                psMap.Add(particleSystem, particleSystem.GetComponent<Renderer>().sharedMaterial.name);
            }
        }

        var anims = curTran.GetComponentsInChildren<Animation>(true);
        foreach (var animation in anims)
        {
            var clips = (from AnimationState animState in animation select animState.clip).ToList();
            if (clips.Count == 0)
            {
                continue;
            }
            animMap.Add(animation, clips.Select(clip => clip.name).ToList());
        }
        Debug.Log("End Init Varibles");
    }

    private Texture2D FindTexture(string texName)
    {
        Texture2D foundTex = null;
        var length = texs.Length;
        for (var i = 0; i < length; i++)
        {
            var tex = texs[i];
            if (tex.name.Contains(texName))
            {
                foundTex = tex;
                break;
            }
        }
        return foundTex;
    }

    private Material[] GetMaterials(List<string> matNames)
    {
        var matFolderPath = GetFolderPath("Materials");
        var count = matNames.Count;
        var mats = new Material[count];
        for (var i = 0; i < count; i++)
        {
            var matName = matNames[i];
            mats[i] =
                AssetDatabase.LoadAssetAtPath(matFolderPath + matName + ".mat", typeof(Material)) as
                Material;
        }
        return mats;
    }

    private string GetFolderPath(string folderName)
    {
        var path = basePath + folderName + "/";
        switch (folderName)
        {
            case "Animations":
                {
                    if (createAnimSubFolder)
                    {
                        path += (curTran.name + "/");
                    }
                    break;
                }
            case "Meshes":
                {
                    if (createMeshSubFolder)
                    {
                        path += (curTran.name + "/");
                    }
                    break;
                }
            case "Materials":
                {
                    if (createMatSubFolder)
                    {
                        path += (curTran.name + "/");
                    }
                    break;
                }
        }

        return path;
    }

    private T LoadAssetAtPath<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
    }

    private void CheckCreatingSubFolders()
    {
        createAnimSubFolder =
            EditorGUILayout.Toggle(
                new GUIContent
                    {
                        tooltip = "Whether or not to create separate anim asset folder per bundle object",
                        text = "Create Anim SubFolder"
                    },
                createAnimSubFolder);

        createMatSubFolder =
            EditorGUILayout.Toggle(
                new GUIContent
                    {
                        tooltip = "Whether or not to create separate mat asset folder per bundle object",
                        text = "Create Mat SubFolder"
                    },
                createMatSubFolder);

        createMeshSubFolder =
            EditorGUILayout.Toggle(
                new GUIContent
                    {
                        tooltip = "Whether or not to create separate mesh asset folder per bundle object",
                        text = "Create Mesh SubFolder"
                    },
                createMeshSubFolder);
    }

    #endregion

    #region Attach Methods

    private void AttachMats()
    {
        Debug.Log("Start AttachMat ....");
        foreach (var renderPair in renderMap)
        {
            renderPair.Key.sharedMaterials = GetMaterials(renderPair.Value);
        }
        foreach (var ps in psMap)
        {
            ps.Key.GetComponent<Renderer>().sharedMaterials = GetMaterials(new List<string> { ps.Value });
        }

        foreach (var skin in skinnedForMatMap)
        {
            skin.Key.sharedMaterials = GetMaterials(skin.Value);
        }

        foreach (var trailPair in trailMap)
        {
            trailPair.Key.sharedMaterials = GetMaterials(trailPair.Value);
        }

        Debug.Log("End AttachMat Succ");
    }

    private void AttachAnims()
    {
        Debug.Log("Start AttachAnim ....");
        var animFolderPath = GetFolderPath("Animations");
        foreach (var animPair in animMap)
        {
            var animation = animPair.Key;
            var defaultName = "";
            if (animation.clip)
            {
                defaultName = animation.clip.name;
            }

            var clips = (from AnimationState animState in animPair.Key select animState.clip).ToList();
            if (clips.Count == 0)
            {
                continue;
            }
            foreach (var animationClip in clips)
            {
                var animName = animationClip.name;
                animation.RemoveClip(animName);
                var assetPath = animFolderPath + animName + ".anim";
                animation.AddClip(LoadAssetAtPath<AnimationClip>(assetPath), animName);
            }
            if (defaultName != "")
            {
                animation.clip = LoadAssetAtPath<AnimationClip>(animFolderPath + defaultName + ".anim");
            }
        }
        Debug.Log("End AttachAnim Succ");
    }

    private void AttachMeshes()
    {
        Debug.Log("Start AttachMesh ....");
        var meshFolderPath = GetFolderPath("Meshes");
        foreach (var filter in filterMap)
        {
            filter.Key.sharedMesh = LoadAssetAtPath<Mesh>(meshFolderPath + filter.Value + ".asset");
        }
        foreach (var skin in skinnedMap)
        {
            skin.Key.sharedMesh = LoadAssetAtPath<Mesh>(meshFolderPath + skin.Value + ".asset");
        }

        Debug.Log("End AttachMesh Succ");
    }

    #endregion

    #region Create Asset Methods

    private void CreatMeshes()
    {
        if(createMeshSubFolder)
        {
            AssetDatabase.CreateFolder(basePath + "Meshes", curTran.name);
        }
        var meshBathPath = GetFolderPath("Meshes");
        foreach (var filter in filterMap)
        {
            var mesh = filter.Key.sharedMesh;
            CreateMeshAsset(mesh, meshBathPath);
        }
        foreach (var skin in skinnedMap)
        {
            var mesh = skin.Key.sharedMesh;
            CreateMeshAsset(mesh, meshBathPath);
        }
    }

    private void CreateMats()
    {
        if (createMatSubFolder)
        {
            AssetDatabase.CreateFolder(basePath + "Materials", curTran.name);
        }
        var matBathPath = GetFolderPath("Materials");
        foreach (var ren in renderMap)
        {
            var mats = ren.Key.sharedMaterials;
            foreach (var mat in mats)
            {
                CreateMatAsset(mat, matBathPath);
            }
        }
        foreach (var ps in psMap)
        {
            var mat = ps.Key.GetComponent<Renderer>().sharedMaterial;
            CreateMatAsset(mat, matBathPath);
        }

        foreach (var skin in skinnedMap)
        {
            var mats = skin.Key.sharedMaterials;
            foreach (var mat in mats)
            {
                CreateMatAsset(mat, matBathPath);
            }
        }
        foreach (var tail in trailMap)
        {
            var mats = tail.Key.sharedMaterials;
            foreach (var mat in mats)
            {
                CreateMatAsset(mat, matBathPath);
            }
        }
    }

    private void CreateAnims()
    {
        if (createAnimSubFolder)
        {
            AssetDatabase.CreateFolder(basePath + "Animations", curTran.name);
        }
        var animBathPath = GetFolderPath("Animations");
        foreach (var anim in animMap)
        {
            var clips = (from AnimationState animState in anim.Key select animState.clip).ToList();
            if (clips.Count == 0)
            {
                continue;
            }
            foreach (var clip in clips)
            {
                CreateAnimAsset(clip, animBathPath);
            }
        }
    }

    private void CreateMatAsset(Material mat, string matBathPath)
    {
        if (mat == null)
        {
            return;
        }
        var path = matBathPath + mat.name + ".mat";
        var isExit = (AssetDatabase.LoadAssetAtPath(path, typeof(Material)) != null);
        if (!isExit)
        {
            var shader = mat.shader;
            var shaderName = shader.name;
            Material cloneMat;
            if (shaderName.StartsWith("Custom"))
            {
                cloneMat = new Material(Shader.Find("Unlit/Texture"));
                var texture = mat.GetTexture("albedoMap");
                if (texture)
                {
                    cloneMat.mainTexture = FindTexture(texture.name);
                }
            }
            else
            {
                cloneMat = new Material(Shader.Find(shaderName));
                if (mat.mainTexture)
                {
                    cloneMat.mainTexture = FindTexture(mat.mainTexture.name);
                }
            }
            AssetDatabase.CreateAsset(cloneMat, path);
        }
    }

    private void CreateAnimAsset(AnimationClip clip, string animBathPath)
    {
        if(clip == null)
        {
            return;
        }
        var path = animBathPath + clip.name + ".anim";
        var isExit = (AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) != null);
        if(!isExit)
        {
            var cloneClip = Instantiate(clip);
            AssetDatabase.CreateAsset(cloneClip, path);
        }
    }

    private void CreateMeshAsset(Mesh mesh, string meshBathPath)
    {
        if (mesh == null)
        {
            return;
        }
        var path = meshBathPath + mesh.name + ".asset";
        var isExit = (AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) != null);
        if (!isExit)
        {
            var cloneMesh = Instantiate(mesh);
            AssetDatabase.CreateAsset(cloneMesh, path);
            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));
            var meshCreated = obj as Mesh;
            if (needAdjustUv)
            {
                var rUvPath = rightMeshUvPath + meshCreated.name + ".obj";
                if (File.Exists(rUvPath))
                {
                    var newUvs = Utils.GetUVsFromFile(rUvPath).ToArray();
                    if (newUvs.Length == meshCreated.uv.Length)
                    {
                        meshCreated.uv = newUvs;
                    }
                }
            }
        }
    }

    #endregion
}
