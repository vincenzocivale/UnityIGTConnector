using UnityEngine;
using System.Collections.Generic;
using openDicom.Registry;

public class MessageTracker : MonoBehaviour
{
    [System.Serializable]
    public class GameObjectMessageTypePair
    {
        public GameObject gameObject;
        public string messageType;
    }

    public List<GameObjectMessageTypePair> gameObjectMessageTypePairs = new List<GameObjectMessageTypePair>();

    private Dictionary<GameObject, string> gameObjectMessageTypes = new Dictionary<GameObject, string>();

    private Dictionary<GameObject, TransformState> lastTransformStates = new Dictionary<GameObject, TransformState>();
    private Dictionary<GameObject, Texture2D> lastTextureStates = new Dictionary<GameObject, Texture2D>();
    private Dictionary<GameObject, string> lastStringStates = new Dictionary<GameObject, string>();

    private void Start()
    {
        // Initialize the dictionaries from the list
        foreach (var pair in gameObjectMessageTypePairs)
        {
            RegisterGameObject(pair.gameObject, pair.messageType);
        }
    }

    public void RegisterGameObject(GameObject modelGO, string messageType)
    {
        if (!gameObjectMessageTypes.ContainsKey(modelGO))
        {
            gameObjectMessageTypes.Add(modelGO, messageType);

            if (messageType == "TRANSFORM")
            {
                lastTransformStates[modelGO] = null; // I dati vengono inizializzati con null per essere inviati la prima volta
            }
            else if (messageType == "IMAGE")
            {
                Renderer renderer = modelGO.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null && renderer.material.mainTexture is Texture2D)
                {
                    lastTextureStates[modelGO] = null;
                }
            }
        }
    }

    public Dictionary<GameObject, string> CheckForChanges()
    {
        Dictionary<GameObject, string> changedObjects = new Dictionary<GameObject, string>();

        foreach (var entry in gameObjectMessageTypes)
        {
            GameObject modelGO = entry.Key;
            string messageType = entry.Value;

            if (messageType == "TRANSFORM")
            {
                TransformState currentState = new TransformState(modelGO.transform.position, modelGO.transform.rotation);
                if (!currentState.Equals(lastTransformStates[modelGO]))
                {
                    changedObjects.Add(modelGO, "TRANSFORM");
                    lastTransformStates[modelGO] = currentState;  // Aggiorna lo stato salvato
                }
            }
            else if (messageType == "IMAGE")
            {
                Renderer renderer = modelGO.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null && renderer.material.mainTexture is Texture2D currentTexture)
                {
                    if (HasTextureChanged(lastTextureStates[modelGO], currentTexture))
                    {
                        changedObjects.Add(modelGO, "IMAGE");
                        lastTextureStates[modelGO] = currentTexture;  // Aggiorna lo stato salvato
                    }
                }
            }
    
        }

        return changedObjects;
    }

    private bool HasTextureChanged(Texture2D lastTexture, Texture2D currentTexture)
    {
        // Logica semplificata per verificare se le texture sono diverse
        if (lastTexture == null || currentTexture == null) return true;
        if (lastTexture.width != currentTexture.width || lastTexture.height != currentTexture.height) return true;

        // Altre verifiche possono essere aggiunte (checksum, confronti dei pixel, ecc.)
        return !lastTexture.Equals(currentTexture);
    }

    private class TransformState
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public TransformState(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public override bool Equals(object obj)
        {
            if (obj is TransformState other)
            {
                return Position == other.Position && Rotation == other.Rotation;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Rotation.GetHashCode();
        }
    }
}
