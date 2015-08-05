using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

class CreateAssetbundles
{
    // This method creates an assetbundle of each SkinnedMeshRenderer
    // found in any selected character fbx, and adds any materials that
    // are intended to be used by the specific SkinnedMeshRenderer.
    [MenuItem("Character Generator/Create Assetbundles")]
    static void Execute()
    {
        bool createdBundle = false;
        foreach (Object o in Selection.GetFiltered(typeof (Object), SelectionMode.DeepAssets))
        {
            if (!(o is GameObject)) continue;
            if (o.name.Contains("@")) continue;
            if (!AssetDatabase.GetAssetPath(o).Contains("/characters/")) continue;

            GameObject characterFBX = (GameObject)o;
            string name = characterFBX.name.ToLower();
           
            Debug.Log("******* Creating assetbundles for: " + name + " *******");

            // Create a directory to store the generated assetbundles.
            if (!Directory.Exists(AssetbundlePath))
                Directory.CreateDirectory(AssetbundlePath);


            // Delete existing assetbundles for current character.
            string[] existingAssetbundles = Directory.GetFiles(AssetbundlePath);
            foreach (string bundle in existingAssetbundles)
            {
                if (bundle.EndsWith(".assetbundle") && bundle.Contains("/assetbundles/" + name))
                    File.Delete(bundle);
            }

            // Save bones and animations to a seperate assetbundle. Any 
            // possible combination of CharacterElements will use these
            // assets as a base. As we can not edit assets we instantiate
            // the fbx and remove what we dont need. As only assets can be
            // added to assetbundles we save the result as a prefab and delete
            // it as soon as the assetbundle is created.
            GameObject characterClone = (GameObject)Object.Instantiate(characterFBX);

			// postprocess animations: we need them animating even offscreen
			foreach (Animation anim in characterClone.GetComponentsInChildren<Animation>())
                anim.animateOnlyIfVisible = false;

            foreach (SkinnedMeshRenderer smr in characterClone.GetComponentsInChildren<SkinnedMeshRenderer>())
                Object.DestroyImmediate(smr.gameObject);
			
            characterClone.AddComponent<SkinnedMeshRenderer>();
            Object characterBasePrefab = GetPrefab(characterClone, "characterbase");
            string path = AssetbundlePath + name + "_characterbase.assetbundle";
            BuildPipeline.BuildAssetBundle(characterBasePrefab, null, path, BuildAssetBundleOptions.CollectDependencies);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(characterBasePrefab));

            // Collect materials.
            List<Material> materials = EditorHelpers.CollectAll<Material>(GenerateMaterials.MaterialsPath(characterFBX));

            // Create assetbundles for each SkinnedMeshRenderer.
            foreach (SkinnedMeshRenderer smr in characterFBX.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                List<Object> toinclude = new List<Object>();

                // Save the current SkinnedMeshRenderer as a prefab so it can be included
                // in the assetbundle. As instantiating part of an fbx results in the
                // entire fbx being instantiated, we have to dispose of the entire instance
                // after we detach the SkinnedMeshRenderer in question.
                GameObject rendererClone = (GameObject)EditorUtility.InstantiatePrefab(smr.gameObject);
                GameObject rendererParent = rendererClone.transform.parent.gameObject;
                rendererClone.transform.parent = null;
                Object.DestroyImmediate(rendererParent);
                Object rendererPrefab = GetPrefab(rendererClone, "rendererobject");
                toinclude.Add(rendererPrefab);

                // Collect applicable materials.
                foreach (Material m in materials)
                    if (m.name.Contains(smr.name.ToLower())) toinclude.Add(m);

                // When assembling a character, we load SkinnedMeshRenderers from assetbundles,
                // and as such they have lost the references to their bones. To be able to
                // remap the SkinnedMeshRenderers to use the bones from the characterbase assetbundles,
                // we save the names of the bones used.
                List<string> boneNames = new List<string>();
                foreach (Transform t in smr.bones)
                    boneNames.Add(t.name);
                string stringholderpath = "Assets/bonenames.asset";
				
				StringHolder holder = ScriptableObject.CreateInstance<StringHolder> ();
				holder.content = boneNames.ToArray();
                AssetDatabase.CreateAsset(holder, stringholderpath);
                toinclude.Add(AssetDatabase.LoadAssetAtPath(stringholderpath, typeof (StringHolder)));

                // Save the assetbundle.
                string bundleName = name + "_" + smr.name.ToLower();
                path = AssetbundlePath + bundleName + ".assetbundle";
                BuildPipeline.BuildAssetBundle(null, toinclude.ToArray(), path, BuildAssetBundleOptions.CollectDependencies);
                Debug.Log("Saved " + bundleName + " with " + (toinclude.Count - 2) + " materials");

                // Delete temp assets.
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(rendererPrefab));
                AssetDatabase.DeleteAsset(stringholderpath);
                createdBundle = true;
            }
        }

        if (createdBundle)
            UpdateCharacterElementDatabase.Execute();
        else
            EditorUtility.DisplayDialog("Character Generator", "No Asset Bundles created. Select the characters folder in the Project pane to process all characters. Select subfolders to process specific characters.", "Ok");
    }

    static Object GetPrefab(GameObject go, string name)
    {
        Object tempPrefab = EditorUtility.CreateEmptyPrefab("Assets/" + name + ".prefab");
        tempPrefab = EditorUtility.ReplacePrefab(go, tempPrefab);
        Object.DestroyImmediate(go);
        return tempPrefab;
    }

    public static string AssetbundlePath
    {
        get { return "Assets" + Path.DirectorySeparatorChar + "assetbundles" + Path.DirectorySeparatorChar; }
    }
}