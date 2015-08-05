using UnityEditor;
using UnityEngine;

class FBXPostprocessor : AssetPostprocessor
{
    // This method is called just before importing an FBX.
    void OnPreprocessModel()
    {
        ModelImporter mi = (ModelImporter)assetImporter;
        mi.globalScale = 1;
        if (!assetPath.Contains("/characters/")) return;
        mi.animationCompression = ModelImporterAnimationCompression.Off;

        // Materials for characters are created using the GenerateMaterials script.
        mi.generateMaterials = 0;
    }

    // This method is called immediately after importing an FBX.
    void OnPostprocessModel(GameObject go)
    {
        if (!assetPath.Contains("/characters/")) return;

        // Assume an animation FBX has an @ in its name,
        // to determine if an fbx is a character or an animation.
        if (assetPath.Contains("@"))
        {
            // For animation FBX's all unnecessary Objects are removed.
            // This is not required but improves clarity when browsing assets.

            // Remove SkinnedMeshRenderers and their meshes.
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Object.DestroyImmediate(smr.sharedMesh, true);
                Object.DestroyImmediate(smr.gameObject);
            }

            // Remove the bones.
            foreach (Transform o in go.transform)
                Object.DestroyImmediate(o);
        }
    }
}