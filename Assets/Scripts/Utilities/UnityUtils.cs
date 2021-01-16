using System.Linq;
using System;
using System.Collections;
using UnityEngine;

namespace RoadGenerator
{
    public static class Utils
    {
        public static float DistanceSquared(Vector3 a, Vector3 b)
        {
            return Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2) + Mathf.Pow(a.z - b.z, 2);
        }
    }
}