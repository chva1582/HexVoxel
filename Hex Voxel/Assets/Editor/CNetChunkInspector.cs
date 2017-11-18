using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CNetChunk))]
public class CNetChunkInspector : Editor
{
    bool nextEdgeButton = false;
    Facet inspectedFacet;
    Vector3 inspectedPoint;

    bool buttonClickThisFrame;

    void OnEnable()
    {

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        CNetChunk chunk = target as CNetChunk;
        buttonClickThisFrame = false;

        if (chunk.net.showNextEdge)
        {
            Vector3 nextStart = chunk.NextStartPos;
            Vector3 nextEnd = chunk.NextEndPos;
            Handles.color = Color.red;
            Handles.DrawLine(nextStart, nextEnd);

            Handles.color = Color.clear;
            Quaternion direction = Quaternion.FromToRotation(Vector3.forward, nextEnd - nextStart);

            if (Handles.Button(nextStart, direction, Vector3.Magnitude(nextEnd - nextStart), 3, Handles.ArrowHandleCap))
                nextEdgeButton = !nextEdgeButton;
            if(nextEdgeButton)
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
                    Handles.Label(chunk.HexToPos(neighbors[i]), chunk.GetNoise(neighbors[i].ToHexCoord()).ToString());
                }
            }
        }

        if (inspectedFacet != null)
            LinesFromFace(inspectedFacet, chunk);

        if (inspectedPoint != null)
            Handles.SphereHandleCap(0, inspectedPoint, Quaternion.identity, 0.2f, EventType.Repaint);

        if (Event.current.type == EventType.MouseDown && !buttonClickThisFrame)
        {
            Vector2 mousePos = new Vector2(Event.current.mousePosition.x, Camera.current.pixelHeight-Event.current.mousePosition.y);
            Ray ray = Camera.current.ScreenPointToRay(mousePos);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (!(meshCollider == null || meshCollider.sharedMesh == null))
                {
                    inspectedPoint = hit.point;
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
                    inspectedFacet = new Facet(chunk.PosToHex(p0).ToHexCell(), chunk.PosToHex(p1).ToHexCell(), chunk.PosToHex(p2).ToHexCell());
                }
            }
        }
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
