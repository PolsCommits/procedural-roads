using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RoadGenerator
{
    public struct Vertex
    {
        public Vector3 pos;
        public int index;
    }

    /// <summary>
    /// Utility class that allows for passing in a Mesh and a number of points and returns a copy of the orignal mesh tiled alongisde those points
    /// </summary>
    public class RoadMesh
    {
        public static Mesh GetRoadMesh(Mesh original, Vector3[] points, Quaternion[] rotations)
        {
            Mesh m = new Mesh();

            // Save the original mesh data
            Vector3[] inputVertices = original.vertices;
            int[] inputTris = original.triangles;
            Vector2[] inputUVs = original.uv;
            Vector3[] inputNormals = original.normals;
            
            // Create containers for the output
            List<Vector3> outputVertices = new List<Vector3>();
            List<int> outputTris = new List<int>();
            List<Vector2> outputUVs = new List<Vector2>();
            List<Vector3> outputNormals = new List<Vector3>();

            // Save the length of the points array for convenience
            int numPoints = points.Length;
            // Save the number of vertices in the original mesh
            int inputVertexCount = inputVertices.Length;
            // Save the number of tris in the original mesh
            int inputTriCount = inputTris.Length;

            // Loop through all the points and duplicate the initial mesh at the corresponding point and rotation
            for(int i = 0; i < numPoints; i++)
            {
                for(int j = 0; j < inputVertexCount; j++)
                {
                    // Create a new vertex by adding the rotated distance from the pivot point to the current spline point
                    Vector3 newVert = points[i] + rotations[i] * inputVertices[j];
                    // Add the new vertex to the output list
                    outputVertices.Add(newVert);
                }

                for(int j = 0; j < inputTriCount; j++)
                {
                    outputTris.Add(inputTris[j] + (inputVertexCount * i));
                }

                outputUVs.AddRange(inputUVs);
                outputNormals.AddRange(inputNormals);
            }

            // For each of the new meshes, join the rear part of each one with the fron of the next one
            for(int i = 0; i < numPoints - 1; i++)
            {
                for(int j = 0; j < inputVertexCount; j++)
                {
                    // If it's a rear vertex
                    if(inputVertices[j].z > 0f)
                    {
                        int mirrored = GetZMirroredVector(inputVertices[j], inputVertices);

                        if (mirrored >= 0)
                            outputVertices[j + (i * inputVertexCount)] = outputVertices[mirrored + ((i + 1) * inputVertexCount)];
                        else
                            outputVertices[j + (i * inputVertexCount)] = outputVertices[j + ((i + 1) * inputVertexCount)];
                    }
                }
            }

            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(outputVertices);
            m.SetTriangles(outputTris, 0);
            m.SetUVs(0, outputUVs);
            m.SetNormals(outputNormals);

            //// Outputs
            //List<Vector3> outputVerts = new List<Vector3>();
            //List<Vector3Int> outputTris = new List<Vector3Int>();
            //List<int> outTris = new List<int>();

            //// Split original in half
            //List<Vertex> frontHalf = new List<Vertex>();
            //List<Vertex> rearHalf = new List<Vertex>();
            //List<Vector3> originalVertices = new List<Vector3>();
            //original.GetVertices(originalVertices);

            //int verticesCount = originalVertices.Count;

            //for (int i = 0; i < originalVertices.Count; i++)
            //{
            //    if (originalVertices[i].z < 0)
            //        frontHalf.Add(new Vertex { pos = originalVertices[i], index = i});
            //    else
            //        rearHalf.Add(new Vertex { pos = originalVertices[i], index = i });
            //}

            //int[] frontIndices = frontHalf.Select((x) => x.index).ToArray();
            //int[] rearIndices = rearHalf.Select((x) => x.index).ToArray();

            //List<Vector3Int> allTris = new List<Vector3Int>();
            //List<Vector3Int> frontTris = new List<Vector3Int>();
            //List<Vector3Int> rearTris = new List<Vector3Int>();
            //List<Vector3Int> middleTris = new List<Vector3Int>();

            //int[] originalTris = original.GetTriangles(0);

            //for (int i = 0; i < originalTris.Length; i+=3)
            //{
            //    Vector3Int tri = new Vector3Int(originalTris[i], originalTris[i + 1], originalTris[i + 2]);
            //    if (frontIndices.Contains(originalTris[i]) &&
            //       frontIndices.Contains(originalTris[i + 1]) &&
            //       frontIndices.Contains(originalTris[i + 2]))
            //    {
            //        frontTris.Add(tri);
            //    }
            //    else if(rearIndices.Contains(originalTris[i]) &&
            //       rearIndices.Contains(originalTris[i + 1]) &&
            //       rearIndices.Contains(originalTris[i + 2]))
            //    {
            //        rearTris.Add(tri);
            //    }
            //    else
            //    {
            //        middleTris.Add(tri);
            //    }

            //    allTris.Add(tri);
            //}

            //int indexOffset = 0;

            //// Add first half to new mesh
            //outputVerts.AddRange(frontHalf.Select(x=>x.pos).ToArray());
            //outputTris.AddRange(frontTris.ToArray());
            
            //// For every point in the input, add a new rear and front half at each given point with the respective rotation
            //for(int i = 0; i < points.Length; i++)
            //{
            //    var newFrontVerts = frontHalf.Select(x => { return x.pos + points[i]; }).ToArray();
            //    // Rotate new face by local rotation
            //    for(int j = 0; j < newFrontVerts.Count(); j++)
            //    {
            //        Vector3 distToPivot = newFrontVerts[j] - points[i];

            //        distToPivot = rotations[i] * distToPivot;

            //        newFrontVerts[j] = distToPivot + points[i];
            //    }

            //    // Add a new set of front vertices, where each point is offset by a position in the input
            //    outputVerts.AddRange(newFrontVerts);

            //    outputTris.AddRange(frontTris.Select(x => { return new Vector3Int(x.x + verticesCount, x.y + verticesCount, x.z + verticesCount); }));
            //}

            //for(int i = 0; i < outputTris.Count; i++)
            //{
            //    outTris.Add(outputTris[i].x);
            //    outTris.Add(outputTris[i].y);
            //    outTris.Add(outputTris[i].z);
            //}

            //m.SetVertices(outputVerts.ToArray());
            //m.SetTriangles(outTris.ToArray(), 0);

            return m;
        }

        // Tries to return the index of a vector that is mirrored on the z axis and within an input array
        static int GetZMirroredVector(Vector3 from, Vector3[] input)
        {
            Vector3 pointToLookFor = from - new Vector3(0, 0, 2 * from.z);

            for(int i = 0; i < input.Length; i++)
            {
                if (input[i] == pointToLookFor)
                    return i;
            }

            return -1;
        }
    }
}
