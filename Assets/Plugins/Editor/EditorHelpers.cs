using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Object=UnityEngine.Object;

class EditorHelpers
{
    // This method loads all files at a certain path and
    // returns a list of specific assets.
    public static List<T> CollectAll<T>(string path) where T : Object
    {
        List<T> l = new List<T>();
        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            if (file.Contains(".meta")) continue;
            T asset = (T) AssetDatabase.LoadAssetAtPath(file, typeof(T));
            if (asset == null) throw new Exception("Asset is not " + typeof(T) + ": " + file);
            l.Add(asset);
        }
        return l;
    }
}