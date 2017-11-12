using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CNetChunk))]
public class CNetChunkInspector : Editor
{
    bool showNextEdge;

    bool buttonCheck = false;

    void OnEnable()
    {

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        showNextEdge = GUILayout.Toggle(showNextEdge, "Show Next Edge");
        serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        CNetChunk chunk = target as CNetChunk;
        Vector3 nextStart = chunk.NextStartPos;
        Vector3 nextEnd = chunk.NextEndPos;
        Handles.color = Color.red;
        if (showNextEdge)
        {
            Handles.DrawLine(nextStart, nextEnd);

            Handles.color = Color.clear;
            Quaternion direction = Quaternion.FromToRotation(Vector3.forward, nextEnd - nextStart);

            if (Handles.Button(nextStart, direction, Vector3.Magnitude(nextEnd - nextStart), 3, Handles.ArrowHandleCap))
                buttonCheck = !buttonCheck;

            if(buttonCheck)
            {
                Handles.color = Color.red;
                List<HexCell> neighbors = chunk.NextNeighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Handles.color = chunk.LegalStatus(new Edge(chunk.NextEdge.ridge, neighbors[i]), chunk.NextEdge.vertex);
                    Handles.CubeHandleCap(0, chunk.HexToPos(neighbors[i]), Quaternion.identity, 0.2f, EventType.Repaint);
                    Handles.Label(chunk.HexToPos(neighbors[i]), chunk.GetNoise(neighbors[i].ToHexCoord()).ToString());
                }
            }
        }
    }
}
