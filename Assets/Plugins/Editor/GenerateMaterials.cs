using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

class GenerateMaterials
{
    // As each SkinnedMeshRenderer that make up a character can be
    // textured with several textures, we cant use the materials
    // Unity generates. This method creates a material for each texture
    // which name contains the name of any SkinnedMeshRenderer present
    // in a any selected character fbx's.
    [MenuItem("Character Generator/Generate Materials")]
    static void Execute()
    {
        bool validMaterial = false;
        foreach (Object o in Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets))
        {
            if (!(o is GameObject)) continue;
            if (o.name.Contains("@")) continue;
            if (!AssetDatabase.GetAssetPath(o).Contains("/characters/")) continue;

            GameObject characterFbx = (GameObject)o;

            // Create directory to store generated materials.
            if (!Directory.Exists(MaterialsPath(characterFbx)))
                Directory.CreateDirectory(MaterialsPath(characterFbx));

            // Collect all textures.
            List<Texture2D> textures = EditorHelpers.CollectAll<Texture2D>(CharacterRoot(characterFbx) + "textures");

            // Create materials for each SkinnedMeshRenderer.
            foreach (SkinnedMeshRenderer smr in characterFbx.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // Check if this SkinnedMeshRenderer has a normalmap.
                Texture2D normalmap = null;
                foreach (Texture2D t in textures)
                {
                    if (!t.name.ToLower().Contains("normal")) continue;
                    if (!t.name.ToLower().Contains(smr.name.ToLower())) continue;
                    normalmap = t;
                    break;
                }

                // Create a Material for each texture which name contains
                // the SkinnedMeshRenderer name.
                foreach (Texture2D t in textures)
                {
                    if (t.name.ToLower().Contains("normal")) continue;
                    if (!t.name.ToLower().Contains(smr.name.ToLower())) continue;
                
                    validMaterial = true;
                    string materialPath = MaterialsPath(characterFbx) + "/" + t.name.ToLower() + ".mat";

                    // Dont overwrite existing materials, as we would
                    // lose existing material settings.
                    if (File.Exists(materialPath)) continue;

                    // Use a default shader according to artist preferences.
                    string shader = "Specular";
                    if (normalmap != null) shader = "Bumped Specular";

                    // Create the Material
                    Material m = new Material(Shader.Find(shader));
                    m.SetTexture("_MainTex", t);
                    if (normalmap != null) m.SetTexture("_BumpMap", normalmap);
                    AssetDatabase.CreateAsset(m, materialPath);
                }
            }
        }
        AssetDatabase.Refresh();
        
        if (!validMaterial) 
            EditorUtility.DisplayDialog("Character Generator", "No Materials created. Select the characters folder in the Project pane to process all characters. Select subfolders to process specific characters.", "Ok");
    }

    // Returns the path to the directory that holds the specified FBX.
    static string CharacterRoot(GameObject character)
    {
        string root = AssetDatabase.GetAssetPath(character);
        return root.Substring(0, root.LastIndexOf('/') + 1);
    }

    // Returns the path to the directory that holds materials generated
    // for the specified FBX.
    public static string MaterialsPath(GameObject character)
    {
		// we will use it only for file enumeration, and separator will be appended for us
		// if we do append here, AssetDatabase will be confused
        return CharacterRoot(character) + "Per Texture Materials";
    }
}