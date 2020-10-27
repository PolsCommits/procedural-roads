using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
    /// <summary>
    /// Just a debugger script, mostly for the RoadMesh
    /// </summary>
    public class RoadDebug : MonoBehaviour
    {
        public Mesh mesh;

        private void Start()
        {
            Vector3[] verts;
            Quaternion[] quats;
            FindObjectOfType<Road>().GetRoadPoints(20, out verts, out quats);
            mesh = RoadMesh.GetRoadMesh(mesh, verts, quats);

            Debug.Log(mesh.triangles.Length);

            if (GetComponent<MeshFilter>())
                GetComponent<MeshFilter>().mesh = mesh;

            if (GetComponent<MeshCollider>())
                GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
