using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;

public class Utils 
{
    public static T[] GetAtPath<T>(string path)
    {
        ArrayList al = new ArrayList();
        string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
        foreach (string fileName in fileEntries)
        {
            int index = fileName.LastIndexOf("\\");
            string localPath = "Assets/" + path;

            if (index > 0)
                localPath += fileName.Substring(index);
            localPath = localPath.Replace("\\", "/");

            Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

            if (t != null)
                al.Add(t);
        }
        T[] result = new T[al.Count];
        for (int i = 0; i < al.Count; i++)
            result[i] = (T)al[i];

        return result;
    }

    public static IEnumerable<Vector2> GetUVsFromFile(string file)
    {
        using (var reader = File.OpenText(file))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("vt"))
                {
                    var vector2 = new Vector2();
                    var strs = line.Split(new[] { ' ' });
                    var index = 0;
                    foreach (var str in strs)
                    {
                        if (index == 1)
                        {
                            vector2.x = float.Parse(str, CultureInfo.InvariantCulture.NumberFormat);
                        }
                        if (index == 2)
                        {
                            vector2.y = float.Parse(str, CultureInfo.InvariantCulture.NumberFormat);
                        }
                        index++;
                    }
                    yield return vector2;
                }
            }
        }
    }
}
