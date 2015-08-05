using System.Collections.Generic;
using UnityEngine;

// This class holds a CharacterElement array and can be saved
// as an asset. This way MonoBehaviours can reference
// it, or it can be added to an assetbundle, making this
// a convenient way of storing procedurally generated
// data on editor time and accessing it at runtime.
public class CharacterElementHolder : ScriptableObject
{
    public List<CharacterElement> content;
}