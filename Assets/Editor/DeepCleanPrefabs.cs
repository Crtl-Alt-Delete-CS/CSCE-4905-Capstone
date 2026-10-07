using UnityEngine;
using UnityEditor;
using System.IO;

public class DeepCleanPrefabs : EditorWindow
{
    [MenuItem("Tools/Deep Clean Biology Cell Prefabs")]
    public static void CleanAllBiologyPrefabs()
    {
        string folderPath = "Assets/BiologyCellsPack/Prefabs";
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        int totalRemoved = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            int removedInThisPrefab = 0;
            Transform[] allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allChildren)
            {
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                removedInThisPrefab += count;
            }

            if (removedInThisPrefab > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                Debug.Log($"Cleaned {removedInThisPrefab} missing script(s) from {Path.GetFileName(path)}");
                totalRemoved += removedInThisPrefab;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Deep cleaning complete! Total missing scripts removed: {totalRemoved}");
    }
}