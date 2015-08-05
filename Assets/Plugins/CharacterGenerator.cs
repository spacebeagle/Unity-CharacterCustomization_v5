using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

// This class can be used to create characters by combining assets.
// The assets are stored in assetbundles to minimize the assets that
// have to be downloaded.
public class CharacterGenerator
{
    // Stores the WWW used to retrieve available CharacterElements stored
    // in the CharacterElementDatabase assetbundle. When storing the available
    // CharacterElements in an assetbundle instead of a ScriptableObject 
    // referenced by a MonoBehaviour, changing the available CharacterElements
    // does not require a client rebuild.
    static WWW database;

    // Stores all CharacterElements obtained from the CharacterElementDatabase 
    // assetbundle, sorted by character and category.
    // character name -> category name -> CharacterElement
    static Dictionary<string, Dictionary<string, List<CharacterElement>>> sortedElements;

    // As elements in a Dictionary are not indexed sequentially we use this list when 
    // determining the previous/next character, instead of sortedElements.
    static List<string> availableCharacters = new List<string>();

    // Stores the WWWs for retrieving the characterbase assetbundles that 
    // hold the bones and animations for a specific character.
    // character name -> WWW for characterbase.assetbundle
    static Dictionary<string, WWW> characterBaseWWWs = new Dictionary<string, WWW>();

    // The bones and animations from the characterbase assetbundles are loaded
    // asynchronously to avoid delays when first using them. A LoadAsync results
    // in an AssetBundleRequest which are stored here so we can check their progress
    // and use the assets they contain once they are loaded.
    // character name -> AssetBundleRequest for Character Base GameObject.
    static Dictionary<string, AssetBundleRequest> characterBaseRequests = new Dictionary<string, AssetBundleRequest>();

    // Stores the currently configured character which is used when downloading
    // assets and generating characters.
    string currentCharacter;

    // Stores the current configuration which is used when downloading assets
    // and generating characters.
    // category name -> current character element
    Dictionary<string, CharacterElement> currentConfiguration = new Dictionary<string, CharacterElement>();

    // Used to give a more accurate download progress.
    float assetbundlesAlreadyDownloaded;
	
    // Avoid users creating instances with a new statement or before
    // sortedElements is populated.
    private CharacterGenerator()
    {
        if (!ReadyToUse) 
            throw new Exception("CharacterGenerator.ReadyToUse must be true before creating CharacterGenerator instances.");
    }

    // The following static methods can be used to create
    // CharacterGenerator instances.
    public static CharacterGenerator CreateWithRandomConfig()
    {
        CharacterGenerator gen = new CharacterGenerator();
        gen.PrepareRandomConfig();
        return gen;
    }

    public static CharacterGenerator CreateWithRandomConfig(string character)
    {
        CharacterGenerator gen = new CharacterGenerator();
        gen.PrepareRandomConfig(character);
        return gen;
    }

    public static CharacterGenerator CreateWithConfig(string config)
    {
        CharacterGenerator gen = new CharacterGenerator();
        gen.PrepareConfig(config);
        return gen;
    }

    // A CharacterGenerator instance can be used to create more then
    // one character. The following methods allow changing the configuration
    // after creating an instance.
    public void PrepareRandomConfig()
    {
        PrepareRandomConfig(availableCharacters[Random.Range(0, availableCharacters.Count)]);
    }

    public void PrepareRandomConfig(string character)
    {
        currentConfiguration.Clear();
        currentCharacter = character.ToLower();
        foreach (KeyValuePair<string, List<CharacterElement>> category in sortedElements[currentCharacter])
            currentConfiguration.Add(category.Key, category.Value[Random.Range(0, category.Value.Count)]);
        UpdateAssetbundlesAlreadyDownloaded();
    }

    // Populates the currentConfiguration from a string to restore
    // saved configurations.
    public void PrepareConfig(string config)
    {
        config = config.ToLower();
        string[] settings = config.Split('|');
        currentCharacter = settings[0];
        currentConfiguration = new Dictionary<string, CharacterElement>();
        for (int i = 1; i < settings.Length; )
        {
            string categoryName = settings[i++];
            string elementName = settings[i++];
            CharacterElement element = null;
            foreach (CharacterElement e in sortedElements[currentCharacter][categoryName])
            {
                if (e.name != elementName) continue;
                element = e;
                break;
            }
            if (element == null) throw new Exception("Element not found: " + elementName);
            currentConfiguration.Add(categoryName, element);
        }
        UpdateAssetbundlesAlreadyDownloaded();
    }

    // Returns the currentConfiguration as a string for easy storage.
    public string GetConfig()
    {
        string s = currentCharacter;
        foreach (KeyValuePair<string, CharacterElement> category in currentConfiguration)
            s += "|" + category.Key + "|" + category.Value.name;
        return s;
    }

    // Sets a random configuration for the next or previous character
    // in availableCharacters.
    public void ChangeCharacter(bool next)
    {
        string character = null;
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            if (availableCharacters[i] != currentCharacter) continue;
            if (next)
                character = i < availableCharacters.Count - 1 ? availableCharacters[i + 1] : availableCharacters[0];
            else
                character = i > 0 ? availableCharacters[i - 1] : availableCharacters[availableCharacters.Count - 1];
            break;
        }
        PrepareRandomConfig(character);
    }

    // Sets the configuration of a category to the next or previous
    // CharacterElement in sortedElements.
    public void ChangeElement(string catagory, bool next)
    {
        List<CharacterElement> available = sortedElements[currentCharacter][catagory];
        CharacterElement element = null;
        for (int i = 0; i < available.Count; i++)
        {
            if (available[i] != currentConfiguration[catagory]) continue;
            if (next)
                element = i < available.Count - 1 ? available[i + 1] : available[0];
            else
                element = i > 0 ? available[i - 1] : available[available.Count - 1];
            break;
        }
        currentConfiguration[catagory] = element;
        UpdateAssetbundlesAlreadyDownloaded();
    }

    // This method downloads the CharacterElementDatabase assetbundle and populates
    // the sortedElements Dictionary from the contents. This is done at runtime as
    // ScriptableObjects do not support Dictionaries. ReadyToUse must be true before
    // you create an instance of CharacterGenerator.
	public static bool ReadyToUse
	{
		get
		{
			if (database == null)
				database = new WWW(AssetbundleBaseURL + "CharacterElementDatabase.assetbundle");

			if (sortedElements != null) return true;
			if (!database.isDone) return false;

			CharacterElementHolder ceh = (CharacterElementHolder) database.assetBundle.mainAsset;

			sortedElements = new Dictionary<string, Dictionary<string, List<CharacterElement>>>();
			foreach (CharacterElement element in ceh.content)
			{
				string[] a = element.bundleName.Split('_');
				string character = a[0];
				string category = a[1].Split('-')[0].Replace(".assetbundle", "");

				if (!availableCharacters.Contains(character))
					availableCharacters.Add(character);

				if (!sortedElements.ContainsKey(character))
					sortedElements.Add(character, new Dictionary<string, List<CharacterElement>>());

				if (!sortedElements[character].ContainsKey(category))
					sortedElements[character].Add(category, new List<CharacterElement>());

				sortedElements[character][category].Add(element);
			}
			return true;
		}
	}

    // Averages the download progress of all assetbundles required for the currentConfiguration,
    // and takes into account the progress at the time of the last configuration change. This 
    // way we can give a progress indication that runs from 0 to 1 even when some assets were
    // already downloaded.
    public float CurrentConfigProgress
    {
        get
        {
            float toDownload = currentConfiguration.Count + 1 - assetbundlesAlreadyDownloaded;
            if (toDownload == 0) return 1;
            float progress = CurrentCharacterBase.progress;
            foreach (CharacterElement e in currentConfiguration.Values)
                progress += e.WWW.progress;
            return (progress - assetbundlesAlreadyDownloaded) / toDownload;
        }
    }

    // Checks to see if all assets required for the currentConfiguration are loaded, and starts
    // the asynchronous loading of the bones and animations if it has not started already.
    // ConfigReady must be true before calling Generate.
    public bool ConfigReady
    {
        get
        {
            if (!CurrentCharacterBase.isDone) return false;

            if (!characterBaseRequests.ContainsKey(currentCharacter))
                characterBaseRequests.Add(currentCharacter, CurrentCharacterBase.assetBundle.LoadAsync("characterbase", typeof(GameObject)));

            if (!characterBaseRequests[currentCharacter].isDone) return false;

            foreach (CharacterElement c in currentConfiguration.Values)
                if (!c.IsLoaded) return false;

            return true;
        }
    }

    // Creates a character based on the currentConfiguration using a newly
    // instantiated character base.
    public GameObject Generate()
    {
        GameObject root = (GameObject)Object.Instantiate(characterBaseRequests[currentCharacter].asset);
        root.name = currentCharacter;
        return Generate(root);
    }

    // Creates a character based on the currentConfiguration recycling a
    // character base, this way the position and animation of the character
    // are not changed.
    public GameObject Generate(GameObject root)
    {
        float startTime = Time.realtimeSinceStartup;

        // The SkinnedMeshRenderers that will make up a character will be
        // combined into one SkinnedMeshRenderers using multiple materials.
        // This will speed up rendering the resulting character.
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> materials = new List<Material>();
        List<Transform> bones = new List<Transform>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>();
        
        foreach (CharacterElement element in currentConfiguration.Values)
        {
            SkinnedMeshRenderer smr = element.GetSkinnedMeshRenderer();
            materials.AddRange(smr.materials);
            for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = smr.sharedMesh;
                ci.subMeshIndex = sub;
                combineInstances.Add(ci);
            }

            // As the SkinnedMeshRenders are stored in assetbundles that do not
            // contain their bones (those are stored in the characterbase assetbundles)
            // we need to collect references to the bones we are using
            foreach (string bone in element.GetBoneNames())
            {
                foreach (Transform transform in transforms)
                {
                    if (transform.name != bone) continue;
                    bones.Add(transform);
                    break;
                }
            }

            Object.Destroy(smr.gameObject);
        }

        // Obtain and configure the SkinnedMeshRenderer attached to
        // the character base.
        SkinnedMeshRenderer r = root.GetComponent<SkinnedMeshRenderer>();
        r.sharedMesh = new Mesh();
        r.sharedMesh.CombineMeshes(combineInstances.ToArray(), false, false);
        r.bones = bones.ToArray();
        r.materials = materials.ToArray();
        
        Debug.Log("Generating character took: " + (Time.realtimeSinceStartup - startTime) * 1000 + " ms");
        return root;
    }

    // This method stores how much of the required assets were already downloaded
    // at the moment of the last configuration change.
    void UpdateAssetbundlesAlreadyDownloaded()
    {
        assetbundlesAlreadyDownloaded = CurrentCharacterBase.progress;
        foreach (CharacterElement e in currentConfiguration.Values)
            assetbundlesAlreadyDownloaded += e.WWW.progress;
    }

    // Returns correct assetbundle base url, whether in the editor, standalone or
    // webplayer, on Mac or Windows.
    public static string AssetbundleBaseURL
    {
        get
        {
            if (Application.platform == RuntimePlatform.WindowsWebPlayer || Application.platform == RuntimePlatform.OSXWebPlayer)
                return Application.dataPath+"/assetbundles/";
            else
                return "file://" + Application.dataPath + "/Assets/assetbundles/";
        }
    }

    // Returns the WWW for retrieving the assetbundle that holds the bones and animations 
    // for currentCharacter, and creates a WWW only if one doesnt exist already. 
    WWW CurrentCharacterBase
    {
        get
        {
            if (!characterBaseWWWs.ContainsKey(currentCharacter))
                characterBaseWWWs.Add(currentCharacter, new WWW(AssetbundleBaseURL + currentCharacter + "_characterbase.assetbundle"));
            return characterBaseWWWs[currentCharacter];
        }
    }
}