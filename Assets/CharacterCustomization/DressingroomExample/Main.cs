using System.Collections;
using UnityEngine;

// This MonoBehaviour is responsible for controlling the CharacterGenerator,
// animating the character, and the user interface. When the user requests a 
// different character configuration the CharacterGenerator is asked to prepare
// the required assets. When all assets are downloaded and loaded a new
// character is created.
class Main : MonoBehaviour
{
    CharacterGenerator generator;
    GameObject character;
    bool usingLatestConfig;
    bool newCharacterRequested = true;
    bool firstCharacter = true;
    string nonLoopingAnimationToPlay;

    const float fadeLength = .6f;
    const int typeWidth = 80;
    const int buttonWidth = 20;
    const string prefName = "Character Generator Demo Pref";

    // Initializes the CharacterGenerator and load a saved config if any.
    IEnumerator Start()
    {
        while (!CharacterGenerator.ReadyToUse) yield return 0;
        if (PlayerPrefs.HasKey(prefName))
            generator = CharacterGenerator.CreateWithConfig(PlayerPrefs.GetString(prefName));
        else
            generator = CharacterGenerator.CreateWithRandomConfig("Female");
    }

    // Requests a new character when the required assets are loaded, starts
    // a non looping animation when changing certain pieces of clothing.
    void Update()
    {
        if (generator == null) return;
        if (usingLatestConfig) return;
        if (!generator.ConfigReady) return;

        usingLatestConfig = true;

        if (newCharacterRequested)
        {
            Destroy(character);
            character = generator.Generate();
            character.animation.Play("idle1");
            character.animation["idle1"].wrapMode = WrapMode.Loop;
            newCharacterRequested = false;

            // Start the walkin animation for the first character.
            if (!firstCharacter) return;
            firstCharacter = false;
            if (character.animation["walkin"] == null) return;
            
            // Set the layer to 1 so this animation takes precedence
            // while it's blended in.
            character.animation["walkin"].layer = 1;
            
            // Use crossfade, because it will also fade the animation
            // nicely out again, using the same fade length.
            character.animation.CrossFade("walkin", fadeLength);
            
            // We want the walkin animation to have full weight instantly,
            // so we overwrite the weight manually:
            character.animation["walkin"].weight = 1;
            
            // As the walkin animation starts outside the camera frustrum,
            // and moves the mesh outside its original bounding box,
            // updateWhenOffscreen has to be set to true for the
            // SkinnedMeshRenderer to update. This should be fixed
            // in a future version of Unity.
            character.GetComponent<SkinnedMeshRenderer>().updateWhenOffscreen = true;
        }
        else
        {
            character = generator.Generate(character);
            
            if (nonLoopingAnimationToPlay == null) return;
            
            character.animation[nonLoopingAnimationToPlay].layer = 1;
            character.animation.CrossFade(nonLoopingAnimationToPlay, fadeLength);
            nonLoopingAnimationToPlay = null;
        }
    }

    void OnGUI()
    {
        if (generator == null) return;
        GUI.enabled = usingLatestConfig && !character.animation.IsPlaying("walkin");
        GUILayout.BeginArea(new Rect(10, 10, typeWidth + 2 * buttonWidth + 8, 500));

        // Buttons for changing the active character.
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(buttonWidth)))
            ChangeCharacter(false);

        GUILayout.Box("Character", GUILayout.Width(typeWidth));

        if (GUILayout.Button(">", GUILayout.Width(buttonWidth)))
            ChangeCharacter(true);

        GUILayout.EndHorizontal();

        // Buttons for changing character elements.
        AddCategory("face", "Head", null);
        AddCategory("eyes", "Eyes", null);
        AddCategory("hair", "Hair", null);
        AddCategory("top", "Body", "item_shirt");
        AddCategory("pants", "Legs", "item_pants");
        AddCategory("shoes", "Feet", "item_boots");

        // Buttons for saving and deleting configurations.
        // In a real world application you probably want store these
        // preferences on a server, but for this demo configurations 
        // are saved locally using PlayerPrefs.
        if (GUILayout.Button("Save Configuration"))
            PlayerPrefs.SetString(prefName, generator.GetConfig());

        if (GUILayout.Button("Delete Configuration"))
            PlayerPrefs.DeleteKey(prefName);

        // Show download progress or indicate assets are being loaded.
        GUI.enabled = true;
        if (!usingLatestConfig)
        {
            float progress = generator.CurrentConfigProgress;
            string status = "Loading";
            if (progress != 1) status = "Downloading " + (int)(progress * 100) + "%";
            GUILayout.Box(status);
        }

        GUILayout.EndArea();
    }

    // Draws buttons for configuring a specific category of items, like pants or shoes.
    void AddCategory(string category, string displayName, string anim)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(buttonWidth)))
            ChangeElement(category, false, anim);

        GUILayout.Box(displayName, GUILayout.Width(typeWidth));

        if (GUILayout.Button(">", GUILayout.Width(buttonWidth)))
            ChangeElement(category, true, anim);

        GUILayout.EndHorizontal();
    }

    void ChangeCharacter(bool next)
    {
        generator.ChangeCharacter(next);
        usingLatestConfig = false;
        newCharacterRequested = true;
    }

    void ChangeElement(string catagory, bool next, string anim)
    {
        generator.ChangeElement(catagory, next);
        usingLatestConfig = false;
        
        if (!character.animation.IsPlaying(anim))
            nonLoopingAnimationToPlay = anim;
    }
}