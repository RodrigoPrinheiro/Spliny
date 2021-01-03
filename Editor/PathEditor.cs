using UnityEngine;
using UnityEditor;

namespace Spliny
{
    [CustomEditor(typeof(PathCreator))]
    public class PathEditor : Editor
    {
        private const float segmentSelectDistanceTreshold = 1f;
        private int selectedSegmentIndex = -1;
        private PathCreator creator;
        private Path path => creator.path;
        public PathData pathSource;

        private void OnEnable()
        {
            creator = (PathCreator)target;
            if (creator.path == null)
                creator.CreatePath();
        }

        private void Input()
        {
            Event guiEvent = Event.current;
            // Get the mouse pos in World space using guiEvent.mousePosition;
            Vector3 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
            {
                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(creator, "Split Segment");
                    path.SplitSegment(mousePos, selectedSegmentIndex);
                }
                else if (!path.Closed)
                {
                    Undo.RecordObject(creator, "Add segment");
                    path.AddSegment(mousePos);
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control)
            {
                Debug.Log("Deleting Segment");
                float minDstToAnchor = 1f;
                int closestAnchor = -1;

                for (int i = 0; i < path.NumPoints; i += 3)
                {
                    Vector3 screenPoint = Handles.matrix.MultiplyPoint(path[i]);
                    float dst = Vector3.Distance(screenPoint, mousePos);
                    Debug.Log(dst);
                    Debug.Log("\n");
                    if (dst < minDstToAnchor)
                    {
                        minDstToAnchor = dst;
                        closestAnchor = i;
                    }
                }

                if (closestAnchor != -1)
                {
                    Undo.RecordObject(creator, "Deleted segment");
                    path.RemoveSegment(closestAnchor);
                }
            }

            if (guiEvent.type == EventType.MouseMove)
            {


                float minDstToSegment = segmentSelectDistanceTreshold;
                int newSelectedSegment = -1;

                for (int i = 0; i < path.Segments; i++)
                {
                    Vector3[] points = path.GetPointsSegment(i);
                    float dst = HandleUtility.DistancePointBezier(mousePos,
                        points[0], points[3], points[1], points[2]);
                    if (dst < minDstToSegment)
                    {
                        minDstToSegment = dst;
                        newSelectedSegment = i;
                    }
                }

                if (newSelectedSegment != selectedSegmentIndex)
                {
                    selectedSegmentIndex = newSelectedSegment;
                    HandleUtility.Repaint();
                }
            }
        }

        private void Draw()
        {
            for (int i = 0; i < path.Segments; i++)
            {
                Vector3[] points = path.GetPointsSegment(i);
                Handles.color = Color.red;
                Handles.DrawDottedLine(points[1], points[0], 3f);
                Handles.DrawDottedLine(points[2], points[3], 3f);
                Color segmentColor = i == selectedSegmentIndex && Event.current.shift ?
                    Color.red : Color.green;

                Handles.DrawBezier(points[0], points[3], points[1], points[2],
                    segmentColor, null, 4f);
            }

            Handles.color = Color.yellow;
            for (int i = 0; i < path.NumPoints; i++)
            {
                Vector3 newPos = Handles.PositionHandle(
                    path[i], Quaternion.identity);
                if (i % 3 == 0)
                    Handles.FreeMoveHandle(path[i], Quaternion.identity, .02f, Vector3.zero, Handles.SphereHandleCap);
                if (newPos != path[i])
                {
                    Undo.RecordObject(creator, "Move Point");
                    path.MovePoint(i, newPos);
                }
            }
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("Create New"))
            {
                Undo.RecordObject(creator, "Create New");
                creator.CreatePath();
            }
            EditorGUILayout.BeginHorizontal();

            pathSource = (PathData)EditorGUILayout.ObjectField
                ("Saved Path",
                pathSource,
                typeof(PathData));

            EditorGUILayout.EndHorizontal();

            if (pathSource != null)
            {
                if (GUILayout.Button("Load Path"))
                {
                    creator.LoadPath(pathSource);
                }
            }

            EditorGUILayout.Space(10f);
        
            bool closed = GUILayout.Toggle(path.Closed, "Close Path");
            if (closed != path.Closed)
            {
                Undo.RecordObject(creator, "Toggle Closed");
                path.Closed = closed;
            }
            bool autosetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
            if (autosetControlPoints != path.AutoSetControlPoints)
            {
                Undo.RecordObject(creator, "Toggle auto set controls");
                path.AutoSetControlPoints = autosetControlPoints;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(15f);
            EditorGUILayout.HelpBox("Save curve to Assets creates a scriptable object of type" +
             " PathData and records the current path positions to it", MessageType.Info);

            // Text field to be used as asset name
            string assetName = EditorGUILayout.TextField("Asset Name", "New Spliny Curve");

            if (GUILayout.Button("Save Curve to Assets", GUILayout.Height(50f)))
            {
                PathData data = CreateAsset<PathData>(assetName);
                // TODO Create scriptable object creation from the inspector
                for (int i = 0; i < path.NumPoints; i++)
                {
                    data.Points.Add(path[i]);
                }

                data.Closed = closed;
            }
        }

        public static T CreateAsset<T>(string assetName = null) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            string assetPathAndName;

            if (assetName == null)
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");
            else
            {
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + $"/{assetName}.asset");
            }

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            return asset;
        }
    }
}
