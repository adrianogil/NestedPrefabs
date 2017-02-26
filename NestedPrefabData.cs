using UnityEngine;
using System;

[Serializable]
public class GameObjectData {

    public int id;

    public string name;

    /// <summary> Relative path on Hierarchy </summary>
    public string hierarchyPath;
    public int hierarchyPathId;

    public float positionX, positionY, positionZ;

    public float rotationX, rotationY, rotationZ;

    public float scaleX, scaleY, scaleZ;

    public void CopyDataTo(Transform transformObject)
    {
        transformObject.localPosition = new Vector3(positionX, positionY, positionZ);
        transformObject.localEulerAngles = new Vector3(rotationX, rotationY, rotationZ);
        transformObject.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

}

[Serializable]
public class NestedPrefabData : GameObjectData {

    /// <summary> Path on assets folder </summary>
    public string prefabPath;
}
