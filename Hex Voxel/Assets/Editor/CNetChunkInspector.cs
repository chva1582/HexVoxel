using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CNetChunk))]
public class CNetChunkInspector : Editor
{
    enum DebugMode { None, SelectFace, SelectedFace, SelectSegment, SelectedSegment, SelectPoint, SelectedPoint, NextEdge, AllMesh};

    CNetChunk chunk;

    bool nextEdgeButton = false;

    Facet inspectedFacet;
    Ridge inspectedRidge;
    Peak inspectedPeak;
    Vector3 cursor;
    DebugMode mode;

    bool showNormals;
    bool showLocations = true;
    bool showValues;

    bool buttonClickThisFrame;
    string iterationString = "0";
    string triIndexString = "";

    void OnEnable()
    {
        chunk = target as CNetChunk;
        if (chunk.net.showNextEdge)
            mode = DebugMode.NextEdge;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        mode = (DebugMode)EditorGUILayout.EnumPopup("Debug Mode", mode);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Normals", GUILayout.Width(60));
        showNormals = EditorGUILayout.Toggle(showNormals, GUILayout.Width(20));
        EditorGUILayout.LabelField("Values", GUILayout.Width(60));
        showValues = EditorGUILayout.Toggle(showValues, GUILayout.Width(20));
        EditorGUILayout.LabelField("Locations", GUILayout.Width(60));
        showLocations = EditorGUILayout.Toggle(showLocations, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        buttonClickThisFrame = false;

        if (mode == DebugMode.NextEdge)
            NextEdge();
        else if (mode == DebugMode.AllMesh)
            AllMesh();

        bool selectedMode = (mode == DebugMode.SelectedFace || mode == DebugMode.SelectedSegment || mode == DebugMode.SelectedPoint);
        if (selectedMode && cursor != null)
            Handles.SphereHandleCap(0, cursor, Quaternion.identity, 0.2f, EventType.Repaint);

        if (mode == DebugMode.SelectedFace)
        {
            Handles.Label(cursor, triIndexString);
            DisplayInfo(new List<HexCell>() { inspectedFacet.first, inspectedFacet.second, inspectedFacet.third });
        }
        else if (mode == DebugMode.SelectedSegment)
            DisplayInfo(new List<HexCell>() { inspectedRidge.start, inspectedRidge.end });
        else if (mode == DebugMode.SelectedPoint)
            DisplayInfo(new List<HexCell>() { inspectedPeak.point });

        Handles.BeginGUI();
        GUI.SetNextControlName("Iteration Input");
        iterationString = EditorGUILayout.TextField(iterationString, GUILayout.Width(150));
        if (GUILayout.Button("Start Constructions", GUILayout.Width(150)) || Event.current.keyCode == KeyCode.Return)
        {
            for (int i = 0; i < int.Parse(iterationString); i++)
            {
                chunk.ConstructFromNextEdge();
            }
            iterationString = "0";
        }
        Handles.EndGUI();

        if (Event.current.keyCode == KeyCode.Tab)
            EditorGUI.FocusTextInControl("Iteration Input");

        if (Event.current.type == EventType.MouseDown && !buttonClickThisFrame)
            {
                if (mode == DebugMode.SelectFace)
                    SelectFace();
                else if (mode == DebugMode.SelectSegment)
                    SelectSegment();
                else if (mode == DebugMode.SelectPoint)
                    SelectPoint();
            }

        if (Event.current.type == EventType.KeyDown)
            KeyCheck();
    }

    void NextEdge()
    {
        Vector3 nextStart = chunk.NextStartPos;
        Vector3 nextEnd = chunk.NextEndPos;

        Handles.color = Color.clear;
        Quaternion direction = Quaternion.FromToRotation(Vector3.forward, nextEnd - nextStart);

        if (Handles.Button(nextStart, direction, Vector3.Magnitude(nextEnd - nextStart), 3, Handles.ArrowHandleCap))
            nextEdgeButton = !nextEdgeButton;
        if (nextEdgeButton)
        {
            Handles.color = Color.red;
            List<HexCell> neighbors = chunk.NextNeighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Handles.color = chunk.LegalStatus(new Edge(chunk.NextEdge.ridge, neighbors[i]), chunk.NextEdge.vertex);
                if (Handles.Button(chunk.HexToPos(neighbors[i]), Quaternion.identity, 0.2f, 0.4f, Handles.CubeHandleCap))
                {
                    chunk.ForcedNextNeighbor = neighbors[i];
                    buttonClickThisFrame = true;
                }
            }
            Handles.color = Color.white;
            DisplayInfo(neighbors);
        }
        inspectedRidge = chunk.NextEdge.ridge;
    }

    void AllMesh()
    {
        DisplayInfo(chunk.verts);
    }

    void DisplayInfo(List<HexCell> displayPoints)
    {
        for (int i = 0; i < displayPoints.Count; i++)
        {
            string outString = "";
            if (showNormals)
            {
                Vector3 normal = chunk.GetNormal(displayPoints[i]).normalized;
                Handles.DrawLine(chunk.HexToPos(displayPoints[i]), chunk.HexToPos(displayPoints[i]) + normal);
            }
            if(showLocations)
                outString += displayPoints[i].ToString();
            if (showValues)
            {
                float value = chunk.GetNoise(displayPoints[i]);
                if (outString.Length > 1)
                    outString += ("\n" + value.ToString());
                else
                    outString += value.ToString();
            }
            Handles.Label(chunk.HexToPos(displayPoints[i]), outString);
        }
    }

    void DisplayInfo(List<Vector3> displayPoints)
    {
        for (int i = 0; i < displayPoints.Count; i++)
        {
            string outString = "";
            HexCell hexPoint = chunk.PosToHex(displayPoints[i]).ToHexCell() + chunk.HexOffset.ToHexCoord().ToHexCell();
            Vector3 point = displayPoints[i] + chunk.PosOffset;
            if (showNormals)
            {
                Vector3 normal = chunk.GetNormal(hexPoint).normalized;
                Handles.DrawLine(point, point + normal);
            }
            if (showLocations)
                outString += hexPoint.ToString();
            if (showValues)
            {
                float value = chunk.GetNoise(hexPoint);
                if (outString.Length > 1)
                    outString += ("\n" + value.ToString());
                else
                    outString += value.ToString();
            }
            Handles.Label(point, outString);
        }
    }

    void SelectFace()
    {
        Vector2 mousePos = new Vector2(Event.current.mousePosition.x, Camera.current.pixelHeight-Event.current.mousePosition.y);
        Ray ray = Camera.current.ScreenPointToRay(mousePos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (!(meshCollider == null || meshCollider.sharedMesh == null))
            {
                cursor = hit.point;
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                triIndexString = hit.triangleIndex.ToString();
                Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
                Transform hitTransform = hit.collider.transform;
                p0 = hitTransform.TransformPoint(p0);
                p1 = hitTransform.TransformPoint(p1);
                p2 = hitTransform.TransformPoint(p2);
                inspectedFacet = new Facet(chunk.PosToHex(p0).ToHexCell(), chunk.PosToHex(p1).ToHexCell(), chunk.PosToHex(p2).ToHexCell());
            }
        }
        mode = DebugMode.SelectedFace;
        Repaint();
    }
    
    void SelectSegment()
    {
        Vector2 mousePos = new Vector2(Event.current.mousePosition.x, Camera.current.pixelHeight - Event.current.mousePosition.y);
        Ray ray = Camera.current.ScreenPointToRay(mousePos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (!(meshCollider == null || meshCollider.sharedMesh == null))
            {
                cursor = hit.point;
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
                Transform hitTransform = hit.collider.transform;
                p0 = hitTransform.TransformPoint(p0);
                p1 = hitTransform.TransformPoint(p1);
                p2 = hitTransform.TransformPoint(p2);

                float firstLineDistance = HandleUtility.DistancePointLine(cursor, p0, p1);
                float secondLineDistance = HandleUtility.DistancePointLine(cursor, p1, p2);
                float thirdLineDistance = HandleUtility.DistancePointLine(cursor, p2, p0);

                if (firstLineDistance <= secondLineDistance && firstLineDistance <= thirdLineDistance)
                    inspectedRidge = new Ridge(chunk.PosToHex(p0).ToHexCell(), chunk.PosToHex(p1).ToHexCell());
                else if (secondLineDistance <= thirdLineDistance)
                    inspectedRidge = new Ridge(chunk.PosToHex(p1).ToHexCell(), chunk.PosToHex(p2).ToHexCell());
                else
                    inspectedRidge = new Ridge(chunk.PosToHex(p2).ToHexCell(), chunk.PosToHex(p0).ToHexCell());
            }
        }
        mode = DebugMode.SelectedSegment;
        Repaint();

    }

    void SelectPoint()
    {
        Vector2 mousePos = new Vector2(Event.current.mousePosition.x, Camera.current.pixelHeight - Event.current.mousePosition.y);
        Ray ray = Camera.current.ScreenPointToRay(mousePos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 1000.0f))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (!(meshCollider == null || meshCollider.sharedMesh == null))
            {
                cursor = hit.point;
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
                Transform hitTransform = hit.collider.transform;
                p0 = hitTransform.TransformPoint(p0);
                p1 = hitTransform.TransformPoint(p1);
                p2 = hitTransform.TransformPoint(p2);

                float firstPointDistance = Vector3.Magnitude(cursor - p0);
                float secondPointDistance = Vector3.Magnitude(cursor - p1);
                float thirdPointDistance = Vector3.Magnitude(cursor - p2);

                if (firstPointDistance <= secondPointDistance && firstPointDistance <= thirdPointDistance)
                    inspectedPeak = new Peak(chunk.PosToHex(p0).ToHexCell());
                else if (secondPointDistance <= thirdPointDistance)
                    inspectedPeak = new Peak(chunk.PosToHex(p1).ToHexCell());
                else
                    inspectedPeak = new Peak(chunk.PosToHex(p2).ToHexCell());
            }
        }
        mode = DebugMode.SelectedPoint;
        Repaint();
    }

    void KeyCheck()
    {
        if (Event.current.keyCode == KeyCode.J)
            showNormals = !showNormals;
        if (Event.current.keyCode == KeyCode.K)
            showValues = !showValues;
        if (Event.current.keyCode == KeyCode.L)
            showLocations = !showLocations;
        else if (Event.current.shift)
        {
            if (Event.current.keyCode == KeyCode.A)
                mode = DebugMode.SelectFace;
            if (Event.current.keyCode == KeyCode.S)
                mode = DebugMode.SelectSegment;
            if (Event.current.keyCode == KeyCode.D)
                mode = DebugMode.SelectPoint;
        }
        if (Event.current.keyCode == KeyCode.N)
            mode = DebugMode.NextEdge;
        if (Event.current.keyCode == KeyCode.M)
            mode = DebugMode.AllMesh;
        Repaint();
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForChunk(CNetChunk chunk, GizmoType gizmoType)
    {
        CNetChunkInspector inspector = Resources.FindObjectsOfTypeAll(typeof(CNetChunkInspector))[0] as CNetChunkInspector;

        Gizmos.color = Color.red;
        switch (inspector.mode)
        {
            case DebugMode.NextEdge:
                Gizmos.DrawLine(chunk.NextEndPos, chunk.NextStartPos);
                break;
            case DebugMode.SelectedFace:
                inspector.DrawFaceGizmo(chunk);
                break;
            case DebugMode.SelectedSegment:
                inspector.DrawLineGizmo(chunk);
                break;
            case DebugMode.SelectedPoint:
                inspector.DrawPointGizmo(chunk);
                break;
            default:
                break;
        }
    }

    public void DrawFaceGizmo(CNetChunk chunk)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>
        {
            chunk.HexToPos(inspectedFacet.first),
            chunk.HexToPos(inspectedFacet.second),
            chunk.HexToPos(inspectedFacet.third)
        };
        mesh.SetVertices(verts);
        mesh.SetTriangles(new int[] { 0, 1, 2 }, 0);
        mesh.RecalculateNormals();
        Gizmos.DrawMesh(mesh);
    }

    public void DrawLineGizmo(CNetChunk chunk)
    {
        Vector3 start = chunk.HexToPos(inspectedRidge.start);
        Vector3 end = chunk.HexToPos(inspectedRidge.end);
        Gizmos.DrawLine(start, end);
    }

    public void DrawPointGizmo(CNetChunk chunk)
    {
        Vector3 point = chunk.HexToPos(inspectedPeak.point);
        Gizmos.DrawSphere(point, 0.1f);
    }

    void LinesFromFace(Facet facet, CNetChunk chunk)
    {
        Handles.color = Color.green;
        Quaternion direction;

        direction = Quaternion.FromToRotation(Vector3.forward, chunk.HexToPos(facet.second) - chunk.HexToPos(facet.first));
        if (Handles.Button(chunk.HexToPos(facet.first), direction, Vector3.Magnitude(chunk.HexToPos(facet.second) - chunk.HexToPos(facet.first)), 3, Handles.ArrowHandleCap))
        {
            Debug.Log(facet.first + " and " + facet.second);
            buttonClickThisFrame = true;
        }

        direction = Quaternion.FromToRotation(Vector3.forward, chunk.HexToPos(facet.third) - chunk.HexToPos(facet.second));
        if (Handles.Button(chunk.HexToPos(facet.second), direction, Vector3.Magnitude(chunk.HexToPos(facet.third) - chunk.HexToPos(facet.second)), 3, Handles.ArrowHandleCap))
        {
            Debug.Log(facet.second + " and " + facet.third);
            buttonClickThisFrame = true;
        }

        direction = Quaternion.FromToRotation(Vector3.forward, chunk.HexToPos(facet.first) - chunk.HexToPos(facet.third));
        if (Handles.Button(chunk.HexToPos(facet.third), direction, Vector3.Magnitude(chunk.HexToPos(facet.first) - chunk.HexToPos(facet.third)), 3, Handles.ArrowHandleCap))
        {
            Debug.Log(facet.third + " and " + facet.first);
            buttonClickThisFrame = true;
        }
    }

    void LinesFromFace(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Handles.color = Color.green;

        Quaternion direction;

        direction = Quaternion.FromToRotation(Vector3.forward, p1 - p0);
        if (Handles.Button(p0, direction, Vector3.Magnitude(p1 - p0), 3, Handles.ArrowHandleCap))
        {
            Debug.Log("First Ridge");
            buttonClickThisFrame = true;
        }

        direction = Quaternion.FromToRotation(Vector3.forward, p2 - p1);
        if (Handles.Button(p1, direction, Vector3.Magnitude(p2 - p1), 3, Handles.ArrowHandleCap))
        {
            Debug.Log("Second Ridge");
            buttonClickThisFrame = true;
        }

        direction = Quaternion.FromToRotation(Vector3.forward, p0 - p2);
        if (Handles.Button(p2, direction, Vector3.Magnitude(p0 - p2), 3, Handles.ArrowHandleCap))
        {
            Debug.Log("Third Ridge");
            buttonClickThisFrame = true;
        }
    }
}
