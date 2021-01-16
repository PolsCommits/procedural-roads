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

        public static Mesh GetRoadProps(Mesh propMesh, Vector3[] points, Quaternion[] rotations, int frequency=4)
        {
            Mesh m = new Mesh();

            // Save the original mesh data
            Vector3[] inputVertices = propMesh.vertices;
            int[] inputTris = propMesh.triangles;
            Vector2[] inputUVs = propMesh.uv;
            Vector3[] inputNormals = propMesh.normals;

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

            int propCount = 0;

            for (int i = 0; i < numPoints; i+=frequency)
            {
                for(int j = 0; j < inputVertexCount; j++)
                {
                    outputVertices.Add(points[i] + rotations[i] * inputVertices[j]);
                }

                for (int j = 0; j < inputTriCount; j++)
                {
                    outputTris.Add(inputTris[j] + (inputVertexCount * propCount));
                }

                outputNormals.AddRange(inputNormals);
                outputUVs.AddRange(inputUVs);
                propCount++;
            }

            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            m.SetVertices(outputVertices);
            m.SetNormals(outputNormals);
            m.SetUVs(0, outputUVs);
            m.SetTriangles(outputTris, 0);

            return m;
        }
    }
}
