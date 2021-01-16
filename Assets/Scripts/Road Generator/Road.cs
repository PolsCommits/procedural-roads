using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RoadGenerator
{
    public enum ControlPointMode
    {
        Free,
        Aligned,
        Mirrored
    }

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
                oneMinusT * oneMinusT * oneMinusT * V3_points[0] +
                3f * oneMinusT * oneMinusT * t * V3_points[1] +
                3f * oneMinusT * t * t * V3_points[2] +
                t * t * t * V3_points[3];
        }

        public Vector3 GetFirstDerivative(float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (V3_points[1] - V3_points[0]) +
                6f * oneMinusT * t * (V3_points[2] - V3_points[1]) +
                3f * t * t * (V3_points[3] - V3_points[2]);
        }

        public void Reset()
        {
            V3_points = new Vector3[] {
            new Vector3(5f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(15f, 0f, 0f),
            new Vector3(20f, 0f, 0f)
            };
        }

        public void Reset(Vector3 startPoint, Vector3 direction)
        {
            V3_points = new Vector3[] {
            startPoint,
            startPoint + direction.normalized * 5f,
            startPoint + direction.normalized * 10f,
            startPoint + direction.normalized * 15f
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
        public float f_MaxHeightForPillar = 30f;
        public float f_MinHeightForPillar = 5f;
        public int i_PropFrequency = 6;
        public AnimationCurve AC_SmoothCurve;
        public Mesh M_RoadMesh;
        public Mesh M_PropMesh;
        public bool b_UsePillars = true;
        public static LayerMask LM_OnlyRoads { get { return LayerMask.GetMask("Road"); } }
        public static LayerMask LM_NoRoads { get { return ~LayerMask.GetMask("Road"); } }
        #endregion

        #region private
        private MeshFilter MF_meshFilter;
        #endregion

        #region serialised
        [SerializeField]
        private ControlPointMode[] CT_types;
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
                curveRes = (int)GetCurveLength(i, curveRes) / 7;

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

        public void GetPillarPoints(int curveRes, out Vector3[] positions, out Quaternion[] rotations, bool circular = true)
        {
            List<Vector3> pos = new List<Vector3>();
            List<Quaternion> rot = new List<Quaternion>();

            for (int i = 0; i < RC_Curves.Count; i++)
            {
                curveRes = (int)GetCurveLength(i, curveRes) / 7;

                for (int j = 0; j <= curveRes; j++)
                {
                    RaycastHit hitInfo;

                    Vector3 point = RC_Curves[i].GetPoint((float)j / curveRes);

                    if (Physics.Raycast(transform.TransformPoint(point), Vector3.down, out hitInfo, 50f))
                    {
                        if (!hitInfo.collider.GetComponent<Terrain>())
                        {
                            continue;
                        }

                        if (Vector3.Distance(transform.TransformPoint(point), hitInfo.point) < 0.3f)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    pos.Add(point);
                    Vector3 vel = GetVelocity((float)j / curveRes, i);
                    vel.y = 0;
                    rot.Add(Quaternion.LookRotation(vel.normalized, Vector3.up));
                }
            }

            if (circular)
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

                    GetRoadPoints(resolution, out points, out quats, false);

                    Mesh m = RoadMesh.GetRoadMesh(M_RoadMesh, points, quats);
                    MF_meshFilter.sharedMesh = m;

                    //Debug.Log("Built road mesh with " + m.vertexCount + " vertices and " + m.triangles.Count() + " triangles");

                    if (GetComponent<MeshCollider>())
                    {
                        GetComponent<MeshCollider>().sharedMesh = m; 
                    }

                    MeshFilter mf = transform.GetChild(0).GetComponent<MeshFilter>();
                    
                    if(b_UsePillars)
                    {

                        if(mf && M_PropMesh)
                        {
                            GetPillarPoints(i_PropFrequency, out points, out quats, false);

                            Mesh m_ = RoadMesh.GetRoadProps(M_PropMesh, points, quats, i_PropFrequency);

                            //Debug.Log("Built prop mesh with " + m_.vertexCount + " vertices and " + m_.triangles.Count() + " triangles");

                            mf.sharedMesh = m_;
                        }
                    }
                    else if(mf)
                    {
                        mf.sharedMesh = null;
                    }
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

        public Vector3 GetTangent(float t, int curveIndex)
        {
            return Quaternion.Euler(0, 90, 0) * GetVelocity(t, curveIndex).normalized;
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
                    if (Physics.Raycast(transform.TransformPoint(RC_Curves[i].GetControlPoint(j)), Vector3.down, out hitInfo, 100f, LM_NoRoads))
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

                    if (Physics.Raycast(pos + Vector3.up * offset, Vector3.down, out hitInfo, 100f, LM_NoRoads))
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

        public void RemoveCurve(int index)
        {
            RC_Curves.RemoveAt(Mathf.Clamp(index, 0, RC_Curves.Count - 1));
        }

        // Length in world units
        public float GetCurveLength(int curveIndex, int resolution=10)
        {
            float length = 0f;

            for(int i = 0; i < resolution - 1; i++)
            {
                length += Vector3.Distance(GetPoint((float)i / resolution, curveIndex), GetPoint(((float)i + 1) / resolution, curveIndex));
            }

            return length;
        }

        public void SetTerrainHeights(PointToElevate[] pointsToElevate, float radius = 3f)
        {
            double start = EditorApplication.timeSinceStartup;

            int size = (int)Mathf.Ceil(radius * 2);
            // Get the first terrain to modify
            Terrain currentTerrain = pointsToElevate[0].terrain;
            int res = currentTerrain.terrainData.heightmapResolution;
            float[,] heights = pointsToElevate[0].terrain.terrainData.GetHeights(0, 0, res, res);
            Vector3 atPosition;
            float dist = 0f;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.zero;

            foreach(var point in pointsToElevate)
            {
                if(point.locationOnTerrain.x < min.x)
                    min.x = point.locationOnTerrain.x;
                else if(point.locationOnTerrain.x > max.x)
                    max.x = point.locationOnTerrain.x;

                if(point.locationOnTerrain.z < min.y)
                    min.y = point.locationOnTerrain.z;
                else if(point.locationOnTerrain.z > max.y)
                    max.y = point.locationOnTerrain.z;
                    
            }

            Debug.Log(min + "\n" + max);

            for(int i = Mathf.FloorToInt(min.y); i < Mathf.CeilToInt(max.y); i++)
            {
                for(int j = Mathf.FloorToInt(min.x); j < Mathf.CeilToInt(max.x); j++)
                {
                    float mini = 99999;
                    PointToElevate closestPoint = pointsToElevate.First();

                    foreach(var point in pointsToElevate)
                    {
                        dist = Vector3.Distance(point.locationOnTerrain, new Vector3(j, 0, i) + point.terrain.transform.position);

                        if(dist < f_RoadWidth / 2)
                        {
                            if(dist < mini)
                            {
                                mini = dist;
                                closestPoint = point;
                            }
                        }
                    }

                    if(mini != 99999)
                        heights[i, j] = AC_SmoothCurve.Evaluate(1 - mini / f_RoadWidth) * closestPoint.height;
                }
            }

            currentTerrain.terrainData.SetHeightsDelayLOD(0, 0, heights);

            Debug.Log("Elevation performed in " + (EditorApplication.timeSinceStartup - start).ToString("0.00") + "s");
        }
    }
}
