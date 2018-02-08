//Collection of CNetChunks that combine to form an entire object
//Applied to the ConstructiveNet Object
using System.Collections.Generic;
using UnityEngine;

public class ConstructiveNet : MonoBehaviour
{
    public static int chunkSize = 16;

    public World world;
    public GameObject ChunkObject;
    public GameObject initializationProbe;
    List<CNetChunk> chunks = new List<CNetChunk>();

    public bool autoGrow;
    public bool constrainToChunk;
    public bool showNextEdge;
    public bool smoothMesh;
    public bool continueOnProblem;

    HexCell initPoint1 = new HexCell(2, 1, 4);
    HexCell initPoint2 = new HexCell(2, 1, 5);
    HexCell initPoint3 = new HexCell(3, 1, 5);

    // Use this for initialization
    void Start ()
    {
        initializationProbe = GameObject.Find("Player");

        CNetChunk initialChunk;
        Ridge initRidge = FindThresholdAlongRay(new Ray(initializationProbe.transform.position, Vector3.down), out initialChunk);
        initialChunk.ConstructFirstTriangle(initRidge);
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyUp(KeyCode.R))
            Restart();
	}

    CNetChunk InitializeChunk(ChunkCoord coords)
    {
        CNetChunk chunk = Instantiate(ChunkObject, World.ChunkToPos(coords), Quaternion.identity).GetComponent<CNetChunk>();
        chunk.transform.SetParent(transform);
        chunk.net = this;
        chunk.chunkCoords = coords;
        return chunk;
    }

    Ridge FindThresholdAlongRay(Ray ray, out CNetChunk chunk)
    {
        float value = 10;
        float distance = 0;
        int i;

        for (int largeSteps = 1; largeSteps < 20; largeSteps++)
        {
            value = world.GetNoise(World.PosToHex(ray.origin + ray.direction * 10 * largeSteps));
            if(value < 0)
            {
                distance = 10 * (largeSteps - 1);
                break;
            }
            if (largeSteps == 19)
                Debug.LogError("Initial Ray Search could not find ground");
        }
        Vector3 realPoint = new Vector3();
        for (i = 0; i < 10; i++)
        {
            realPoint = ray.origin + ray.direction * (distance + i);
            value = world.GetNoise(World.PosToHex(realPoint));
            if (value < 0)
                break;
        }

        chunk = InitializeChunk(CNetChunk.PosToChunk(realPoint));
        chunks.Add(chunk);
        HexCoord hitPoint = chunk.PosToHex(realPoint);
        //print(realPoint);
        //print("Hex Coordinates: " + World.PosToHex(realPoint));
        //print("Chunk Coordinates: " + CNetChunk.PosToChunk(realPoint));
        Ridge ridge = new Ridge();
        ridge.start.X = (sbyte)Mathf.FloorToInt(hitPoint.x);
        ridge.start.Y = (sbyte)Mathf.RoundToInt(hitPoint.y);
        ridge.start.Z = (sbyte)Mathf.RoundToInt(hitPoint.z);
        ridge.end.X = (sbyte)Mathf.CeilToInt(hitPoint.x);
        ridge.end.Y = (sbyte)Mathf.RoundToInt(hitPoint.y);
        ridge.end.Z = (sbyte)Mathf.RoundToInt(hitPoint.z);
        return ridge;
    }

    //This along with the method call in update should go in World as soon as the old system is past its use and archived
    void Restart()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);

        print("Restarted");
        chunks[0].Restart();
        chunks[0].ConstructFirstTriangle(initPoint1, initPoint2, initPoint3);
    }
}