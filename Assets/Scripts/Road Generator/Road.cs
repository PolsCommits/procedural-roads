using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
    [System.Serializable]
    public class RoadCurve
    {
        #region serialised
        [SerializeField]
        private Vector3[] V3_points;
        #endregion

        public int NumberOfPoints { get { return V3_points.Length; } }

        public Vector3 GetControlPoint(int index) { return V3_points[index]; }
        public void SetControlPoint(int index, Vector3 point) { V3_points[index] = point; }

        // Kudos to https://catlikecoding.com/unity/tutorials/curves-and-splines/ for providing the foundation of generating these splines
        public Vector3 GetPoint(float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * V3_points[0] +
                2f * oneMinusT * t * V3_points[1] +
                t * t * V3_points[2];
        }

        public Vector3 GetFirstDerivative(float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                2f * oneMinusT * (V3_points[1] - V3_points[0]) +
                2f * t * (V3_points[2] - V3_points[1]);
        }

        public void Reset()
        {
            V3_points = new Vector3[] {
            new Vector3(5f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(15f, 0f, 0f)
            };
        }

        public void Reset(Vector3 startPoint, Vector3 direction)
        {
            V3_points = new Vector3[] {
            startPoint,
            startPoint + direction.normalized * 5f,
            startPoint + direction.normalized * 10f
            };
        }
    }

    [ExecuteAlways]
    /// <summary>
    /// MonoBehaviour that allows for the creation of 3D road meshes
    /// </summary>
    public class Road : MonoBehaviour
    {
        #region public
        public List<RoadCurve> RC_Curves;
        public float f_RoadWidth = 12f;
        public AnimationCurve AC_SmoothCurve;
        public Mesh M_RoadMesh;
        #endregion

        #region private
        private MeshFilter MF_meshFilter;
        #endregion

        public void AddCurve()
        {
            RoadCurve rc = new RoadCurve();
            if (RC_Curves.Count == 0)
                rc.Reset();
            else
                rc.Reset(RC_Curves[RC_Curves.Count - 1].GetControlPoint(RC_Curves[RC_Curves.Count - 1].NumberOfPoints - 1), GetVelocity(1, RC_Curves.Count - 1));
            RC_Curves.Add(rc);
        }

        // Returns all the points on the spline, given a specific curve resolution
        public void GetRoadPoints(int curveRes, out Vector3[] positions, out Quaternion[] rotations, bool circular=true)
        {
            List<Vector3> pos = new List<Vector3>();
            List<Quaternion> rot = new List<Quaternion>();

            for (int i = 0; i < RC_Curves.Count; i++)
            {
                for (int j = 0; j <= curveRes; j++)
                {
                    pos.Add(RC_Curves[i].GetPoint((float)j / curveRes));
                    rot.Add(Quaternion.LookRotation(GetVelocity((float)j / curveRes, i).normalized));
                }
            }

            if(circular)
            {
                pos.Add(pos.First());
                rot.Add(rot.First());
            }

            rotations = rot.ToArray();
            positions = pos.ToArray();
        }

        public void BuildMesh(int resolution=10)
        {
            if(!MF_meshFilter)
                MF_meshFilter = GetComponent<MeshFilter>();

            if(MF_meshFilter)
            {
                if(M_RoadMesh)
                {
                    Vector3[] points;
                    Quaternion[] quats;

                    GetRoadPoints(resolution, out points, out quats);

                    Mesh m = RoadMesh.GetRoadMesh(M_RoadMesh, points, quats);
                    MF_meshFilter.sharedMesh = m;

                    if (GetComponent<MeshCollider>())
                        GetComponent<MeshCollider>().sharedMesh = m;
                }
            }
        }

        public void ResetCurves()
        {
            RC_Curves.Clear();
        }

        public Vector3 GetPoint(float t, int curveIndex)
        {
            return transform.TransformPoint(RC_Curves[curveIndex].GetPoint(t));
        }

        public Vector3 GetVelocity(float t, int curveIndex)
        {
            return transform.TransformPoint(
                RC_Curves[curveIndex].GetFirstDerivative(t)) - transform.position;
        }

        public void Reset(int index)
        {
            RC_Curves[index].Reset();
        }

        RaycastHit hitInfo;

        public void MatchElevation()
        {
            for (int i = 0; i < RC_Curves.Count; i++)
            {
                for (int j = 0; j < RC_Curves[i].NumberOfPoints; j++)
                {
                    if (Physics.Raycast(transform.TransformPoint(RC_Curves[i].GetControlPoint(j)), Vector3.down, out hitInfo, 100f))
                    {
                        Debug.DrawLine(transform.TransformPoint(RC_Curves[i].GetControlPoint(j)), hitInfo.point, Color.green, 5f);
                        RC_Curves[i].SetControlPoint(j, transform.InverseTransformPoint(hitInfo.point));
                    }
                    else
                    {
                        Debug.DrawRay(transform.TransformPoint(RC_Curves[i].GetControlPoint(j)), Vector3.down * 100f, Color.red, 5f);
                    }
                }
            }
        }

        private float offset = 2f;

        public struct PointToElevate
        {
            public Vector3 locationOnTerrain;
            public float height;
            public Terrain terrain;
        }

        public Terrain ElevateTerrain(int resolution = 100)
        {
            Terrain t = null;
            List<PointToElevate> pointsToElevate = new List<PointToElevate>();

            for (int i = 0; i < RC_Curves.Count; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    //Vector3 pos = RC_curves[i].GetControlPoint(j) + transform.position;
                    Vector3 pos = GetPoint((float)j / resolution, i);

                    if (Physics.Raycast(pos + Vector3.up * offset, Vector3.down, out hitInfo, 100f))
                    {
                        t = hitInfo.collider.gameObject.GetComponent<Terrain>() != null ? hitInfo.collider.gameObject.GetComponent<Terrain>() : t;

                        if (t)
                        {
                            float worldToTerrainRatio = 1f / t.terrainData.heightmapScale.x;
                            pointsToElevate.Add(new PointToElevate { locationOnTerrain = (hitInfo.point - t.transform.position) * worldToTerrainRatio, height = Remap(pos.y, 0, t.terrainData.heightmapScale.y, 0, 1), terrain = t });
                            //SetTerrainHeight(hitInfo.point * worldToTerrainRatio, Remap(pos.y, 0, t.terrainData.heightmapScale.y, 0, 1), t);
                        }
                    }
                }
            }

            if (t)
            {
                // Sort all the points by the terrain they belong to
                pointsToElevate.Sort((x, y) => x.terrain.groupingID.CompareTo(y.terrain.groupingID));

                SetTerrainHeights(pointsToElevate.ToArray(), f_RoadWidth);
            }

            return t;
        }

        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public void SetTerrainHeights(PointToElevate[] pointsToElevate, float radius = 3f)
        {
            int size = (int)Mathf.Ceil(radius * 2);
            // Get the first terrain to modify
            Terrain currentTerrain = pointsToElevate[0].terrain;
            int res = currentTerrain.terrainData.heightmapResolution;
            float[,] heights = pointsToElevate[0].terrain.terrainData.GetHeights(0, 0, res, res);
            Vector3 atPosition;
            float dist = 0f;

            for (int n = 0; n < pointsToElevate.Length; n++)
            {
                // If we have reached a new terrain, apply the changes to the current terrain
                if (pointsToElevate[n].terrain != currentTerrain)
                {
                    currentTerrain.terrainData.SetHeightsDelayLOD(0, 0, heights);
                    currentTerrain = pointsToElevate[n].terrain;
                    res = currentTerrain.terrainData.heightmapResolution;
                    heights = pointsToElevate[n].terrain.terrainData.GetHeights(0, 0, res, res);
                }

                // Get the position to elevate around
                atPosition = pointsToElevate[n].locationOnTerrain;

                for (int i = Mathf.Max((int)atPosition.z - size / 2, 0); i < Mathf.Min(res, (int)atPosition.z + size / 2); i++)
                {
                    for (int j = Mathf.Max(0, (int)atPosition.x - size / 2); j < Mathf.Min(res, (int)atPosition.x + size / 2); j++)
                    {
                        dist = Mathf.Sqrt(Mathf.Pow(atPosition.z - i, 2) + Mathf.Pow(atPosition.x - j, 2));
                        if (dist < radius)
                            heights[i, j] = Mathf.Max(AC_SmoothCurve.Evaluate(1f - dist / radius) * pointsToElevate[n].height, heights[i, j]);
                    }
                }
            }

            currentTerrain.terrainData.SetHeightsDelayLOD(0, 0, heights);
        }
    }
}
