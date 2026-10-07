using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MissingScriptCleaner : Editor
{
    [MenuItem("Tools/Clean Missing Scripts in Scene")]
    public static void CleanSceneMissingScripts()
    {
        // Find all GameObjects in the active scene, including inactive ones
        GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int totalRemoved = 0;

        foreach (GameObject go in allGameObjects)
        {
            // GameObjectUtility handles the "nuclear option" for null components
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removedCount > 0)
            {
                totalRemoved += removedCount;
                EditorUtility.SetDirty(go);
                Debug.Log($"Removed {removedCount} missing script(s) from GameObject: {go.name}", go);
            }
        }

        Debug.Log($"🧼 Clean up complete! Total missing scripts removed from scene: {totalRemoved}");
    }
}
