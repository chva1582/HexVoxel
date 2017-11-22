﻿using System.Collections.Generic;
using UnityEngine;

public class ConstructiveNet : MonoBehaviour
{
    public World world;
    public GameObject ChunkObject;
    List<CNetChunk> chunks = new List<CNetChunk>();

    public bool autoGrow;
    public bool showNextEdge;
    public bool smoothMesh;
    public bool continueOnProblem;

    HexCell initPoint1 = new HexCell(2, 1, 4);
    HexCell initPoint2 = new HexCell(2, 1, 5);
    HexCell initPoint3 = new HexCell(3, 1, 5);

    // Use this for initialization
    void Start ()
    {
        CNetChunk initialChunk = InitializeChunk(new ChunkCoord(-2,0,3));
        chunks.Add(initialChunk);

        initialChunk.BuildFirstTriangle(initPoint1, initPoint2, initPoint3);
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyUp(KeyCode.R))
            Restart();
	}

    CNetChunk InitializeChunk(ChunkCoord coords)
    {
        CNetChunk chunk = Instantiate(ChunkObject, World.ChunkToPos(coords), Quaternion.Euler(Vector3.up)).GetComponent<CNetChunk>();
        chunk.transform.SetParent(transform);
        chunk.net = this;
        chunk.chunkCoords = coords;
        return chunk;
    }

    //This along with the method call in update should go in World as soon as the old system is past its use and archived
    void Restart()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);

        print("Restarted");
        chunks[0].Restart();
        chunks[0].BuildFirstTriangle(initPoint1, initPoint2, initPoint3);
    }
}