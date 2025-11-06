using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class CollisionExporter : MonoBehaviour
{
    [System.Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class CollisionData
    {
        public string type;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public Vector3Data size;
        public string meshName;
    }

    [System.Serializable]
    public class CollisionCollection
    {
        public List<CollisionData> colliders = new List<CollisionData>();
    }

    [ContextMenu("Export Collisions")]
    public void ExportCollisions()
    {
        CollisionCollection data = new CollisionCollection();

        foreach (var col in FindObjectsOfType<Collider>())
        {
            if (col.gameObject.tag != "Server/ServerCollision")
                continue;

            CollisionData cd = new CollisionData();
            cd.position = new Vector3Data(col.transform.position);
            cd.rotation = new Vector3Data(col.transform.eulerAngles);
            cd.scale = new Vector3Data(col.transform.lossyScale);

            if (col is BoxCollider box)
            {
                cd.type = "box";
                cd.size = new Vector3Data(Vector3.Scale(box.size, col.transform.lossyScale));
            }
            else if (col is SphereCollider sphere)
            {
                cd.type = "sphere";
                float maxScale = Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y, col.transform.lossyScale.z);
                cd.size = new Vector3Data(Vector3.one * sphere.radius * 2f * maxScale);
            }
            else if (col is MeshCollider meshCol)
            {
                cd.type = "mesh";
                cd.meshName = meshCol.sharedMesh != null ? meshCol.sharedMesh.name : "unknown";
                cd.size = new Vector3Data(Vector3.one);
            }
            else
            {
                cd.type = "unknown";
                cd.size = new Vector3Data(Vector3.one);
            }

            data.colliders.Add(cd);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string path = Path.Combine(Application.dataPath, "ServerCollisions.json");
        File.WriteAllText(path, json);
        Debug.Log($"Collisions exported! Path: {path}");
    }
}
