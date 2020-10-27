using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RoadGenerator
{
    [CustomEditor(typeof(RoadDebug))]
    public class RoadDebugEditorWindow : Editor
    {
        #region private
        RoadDebug RD_roadDebug;
        Transform T_debugTransform;
        Quaternion Q_debugRotation;
        #endregion

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private float f_pointSize = 0.25f;

        private void OnSceneGUI()
        {
            return;

            RD_roadDebug = target as RoadDebug;


            T_debugTransform = RD_roadDebug.transform;
            Q_debugRotation = Tools.pivotRotation == PivotRotation.Local ?
                T_debugTransform.rotation : Quaternion.identity;

            Vector3[] verts = RD_roadDebug.mesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                RenderPoint(verts[i]);
            }
        }

        private void RenderPoint(Vector3 point)
        {
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, T_debugTransform.TransformPoint(point), Q_debugRotation, f_pointSize, EventType.Repaint);
        }
    }
}
