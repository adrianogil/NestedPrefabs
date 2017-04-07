using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public interface IPrefabGeneration
{
    GameObject GenerateFrom(string prefabPath);
}

public class NestedPrefab : MonoBehaviour {

    [HideInInspector]
    [SerializeField]
    public List<NestedPrefabData> nestedPrefabsData;

    [HideInInspector]
    [SerializeField]
    public List<GameObjectData> emptyObjectsData;

    private const string pathSeparator = "//Nested//";

    private Dictionary<int, Transform> hierarchyDict;

    public IPrefabGeneration prefabGenerator = null;

    void Awake()
    {
        if (prefabGenerator == null) {
            prefabGenerator = new DefaultPrefabGeneration();
        }
    }

	// Use this for initialization
	public void SavePrefabData () {
        nestedPrefabsData = new List<NestedPrefabData>();
        emptyObjectsData = new List<GameObjectData>();

        currentId = 0;

        SavePrefabData(transform, "", currentId);
	}

    private static int currentId = 0; // Id 0 corresponds to Prefab Root

    public void SavePrefabData (Transform trans, string relativePath, int pathId) {
        Transform child;
        GameObject prefabParent;

#if UNITY_EDITOR
        for (int i = 0; i < trans.childCount; i++)
        {
            child = trans.GetChild(i);

            prefabParent = PrefabUtility.GetPrefabParent(child.gameObject) as GameObject;

            if (prefabParent == null)
            {
                GameObjectData goData = new GameObjectData();

                currentId++;
                goData.id = currentId;

                goData.hierarchyPath = relativePath;
                goData.hierarchyPathId = pathId;

                goData.CopyFrom(child);

                emptyObjectsData.Add(goData);

                SavePrefabData(child, relativePath + pathSeparator + child.name, goData.id);
            }
            else {
                NestedPrefabData data = new NestedPrefabData();

                data.prefabPath = AssetDatabase.GetAssetPath(prefabParent);
                // Debug.Log("Prefab path = " + data.prefabPath);
                data.hierarchyPath = relativePath;
                // Debug.Log("prefabHierarchyPath " + data.hierarchyPath);

                data.hierarchyPathId = pathId;

                data.CopyFrom(child);

                nestedPrefabsData.Add(data);
            }
        }
#endif
    }

    /// <summary>
    /// Main method to generate prefabs using all saved data
    /// Attention: All children objects are going to be removed
    /// </summary>
    public void GeneratePrefabs(bool askPermission = true)
    {
    #if UNITY_EDITOR
        if (askPermission && !EditorUtility.DisplayDialog("Do want to update children objects?",
                "Are you sure you want to update children objects? All current changes would be lost", "Yes", "I am not sure"))
        {
            return;
        }

        // Store the states of 'root' and its children.
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "NestedPrefab update");
    #endif

        // Destroy all children objects in order to recreate them according to internal data
        DestroyChildren();

        // Create empty objects
        UpdateTransformFromIds();

        // Generate all prefabs
        for (int i = 0; i < nestedPrefabsData.Count; i++)
        {
            GeneratePrefab(nestedPrefabsData[i]);
        }
    }


    void GeneratePrefab(NestedPrefabData prefabData)
    {
        // Debug.Log("NestedPrefab::GeneratePrefab - prefabData " + prefabData.prefabPath + " ");

        Transform parent = GetHierarchyTransform(prefabData.hierarchyPathId);

        GameObject clone = prefabGenerator.GenerateFrom(prefabData.prefabPath);

        if (clone == null)
        {
            Debug.LogError("Error when trying to generate prefab " + prefabData.prefabPath + " !", this);
        }

        clone.transform.parent = parent;

        prefabData.CopyDataTo(clone.transform);

        // Recursive generation
        NestedPrefab nestedPrefab = clone.GetComponent<NestedPrefab>();
        if (nestedPrefab != null)
        {
            nestedPrefab.prefabGenerator = prefabGenerator;
            nestedPrefab.GeneratePrefabs(false); // Avoid to ask permission twice
        }
    }

    void UpdateTransformFromIds()
    {
        hierarchyDict = new Dictionary<int, Transform>();
        hierarchyDict.Add(0, transform);

        UpdateTransformFromIds(0, transform);
    }

    void UpdateTransformFromIds(int id, Transform parent)
    {
        Transform empty;

        for (int i = 0; i < emptyObjectsData.Count; i++)
        {
            if (emptyObjectsData[i].hierarchyPathId == id) {
                empty = CreateEmpty(emptyObjectsData[i], parent);
                hierarchyDict.Add(emptyObjectsData[i].id, empty);
                UpdateTransformFromIds(emptyObjectsData[i].id, empty);
            }
        }
    }

    Transform CreateEmpty(GameObjectData data, Transform parent)
    {
        GameObject newGo = new GameObject(data.name);

        newGo.transform.parent = parent;
        data.CopyDataTo(newGo.transform);

        return newGo.transform;
    }

    Transform GetHierarchyTransform(int hierarchyPathId) {

        return hierarchyDict[hierarchyPathId];
    }

    void DestroyChildren()
    {
        for (int i = transform.childCount-1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}

public class DefaultPrefabGeneration : IPrefabGeneration
{

    public GameObject GenerateFrom(string prefabPath) {
        GameObject prefab = null, clone = null;

    #if UNITY_EDITOR
        if (!Application.isPlaying) {
            prefab =
              (AssetDatabase.LoadAssetAtPath(prefabPath, typeof(Transform)) as Transform).gameObject;

            clone = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
    #else
        prefab = Resources.Load(prefabPath) as GameObject;
        clone = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
    #endif

        return clone;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(NestedPrefab))]
public class NestedPrefabEditor : Editor {

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NestedPrefab nestedPrefab = target as NestedPrefab;

        if (nestedPrefab == null) return;

        nestedPrefab.prefabGenerator = new DefaultPrefabGeneration();

        if (GUILayout.Button("Save Nested Prefabs")) {
            nestedPrefab.SavePrefabData();
        }

        if (GUILayout.Button("Revert Prefabs")) {
            nestedPrefab.GeneratePrefabs();
        }
    }

}
#endif