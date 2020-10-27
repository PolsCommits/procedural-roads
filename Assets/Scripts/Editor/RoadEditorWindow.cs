using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RoadGenerator
{
    [CustomEditor(typeof(Road))]
    public class RoadEditorWindow : Editor
    {
        private Road R_road;
        private Transform T_handleTransform;
        private Quaternion Q_handleRotation;

        private SerializedProperty SP_AnimationCurve;
        private SerializedProperty SP_RoadWidth;

        private const int i_curveResolution = 10;

        public override void OnInspectorGUI()
        {
            R_road = (Road)target;

            SP_AnimationCurve = serializedObject.FindProperty("AC_SmoothCurve");
            SP_RoadWidth = serializedObject.FindProperty("f_RoadWidth");

            EditorGUILayout.PropertyField(SP_RoadWidth, new GUIContent("Road width: "));
            EditorGUILayout.PropertyField(SP_AnimationCurve, new GUIContent("Elevation smooth pattern"), GUILayout.Height(20f));

            serializedObject.ApplyModifiedProperties();

            if (selectedCurveIndex >= 0 && selectedCurveIndex >= 0 &&
                selectedCurveIndex < R_road.RC_Curves.Count &&
                selectedPointIndex < R_road.RC_Curves[selectedCurveIndex].GetNumberOfPoints)
            {
                DrawSelectedPointInspector();
            }

            if(GUILayout.Button("Match elevation"))
            {
                Undo.RecordObject(R_road, "Match road elevation to terrain");
                R_road.MatchElevation();
            }

            if(GUILayout.Button("Elevate terrain to road heights"))
            {
                Undo.RecordObject(R_road, "Elevate terrain to road heights");
                R_road.ElevateTerrain();
            }

            if (GUILayout.Button("Add curve"))
            {
                Undo.RecordObject(R_road, "Add new curve");
                R_road.AddCurve();
                // Code goes here
            }

            if (GUILayout.Button("Remove curves"))
            {
                Undo.RecordObject(R_road, "Remove all curves");
                R_road.ResetCurves();
                // Code goes here
            }
        }

        private void OnSceneGUI()
        {
            R_road = target as Road;
            T_handleTransform = R_road.transform;
            Q_handleRotation = Tools.pivotRotation == PivotRotation.Local ?
                T_handleTransform.rotation : Quaternion.identity;

            for(int i = 0; i < R_road.RC_Curves.Count; i++)
            {
                Vector3 p0 = ShowPoint(0, i);
                Vector3 p1 = ShowPoint(1, i);
                Vector3 p2 = ShowPoint(2, i);
                Vector3 p3 = ShowPoint(3, i);

                Handles.color = Color.gray;
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p1, p2);
                Handles.DrawLine(p2, p3);

                Handles.color = Color.white;
                Vector3 lineStart = R_road.GetPoint(0, i);
                
                for(int j = 1; j <= i_curveResolution; j++)
                {
                    Vector3 lineEnd = R_road.GetPoint(j / (float)i_curveResolution, i);
                    Handles.DrawLine(lineStart, lineEnd);
                    lineStart = lineEnd;
                }
            }
        }

        private const float handleSize = 0.04f;
        private const float pickSize = 0.06f;

        private int selectedPointIndex = -1;
        private int selectedCurveIndex = -1;

        private Vector3 ShowPoint(int pointIndex, int curveIndex)
        {
            Vector3 point = T_handleTransform.TransformPoint(R_road.RC_Curves[curveIndex].GetControlPoint(pointIndex));

            float size = HandleUtility.GetHandleSize(point);
            Handles.color = Color.white;
            if(Handles.Button(point, Q_handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
            {
                selectedPointIndex = pointIndex;
                selectedCurveIndex = curveIndex;
                // Refreshes inspector display
                Repaint();
            }

            if(selectedCurveIndex == curveIndex && selectedPointIndex == pointIndex)
            {
                EditorGUI.BeginChangeCheck();
                point = Handles.DoPositionHandle(point, Q_handleRotation);
            
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(R_road, "Move Point");
                    EditorUtility.SetDirty(R_road);
                    R_road.RC_Curves[curveIndex].SetControlPoint(pointIndex, T_handleTransform.InverseTransformPoint(point));
                }
            }

            return point;
        }

        private void DrawSelectedPointInspector()
        {
            GUILayout.Label("Selected Point");
            EditorGUI.BeginChangeCheck();
            Vector3 point = EditorGUILayout.Vector3Field("Position", R_road.RC_Curves[selectedCurveIndex].GetControlPoint(selectedPointIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(R_road, "Move Point");
                EditorUtility.SetDirty(R_road);
                R_road.RC_Curves[selectedCurveIndex].SetControlPoint(selectedPointIndex, point);
            }
        }
    }
}
