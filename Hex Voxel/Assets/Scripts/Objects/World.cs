using UnityEngine;

public enum DebugMode { None, Octahedron, Gradient }

public class World : MonoBehaviour
{
    public bool pointLoc;
    public bool show;
    public float size;
    public GameObject chunk;

    public DebugMode debugMode = DebugMode.None;

    // Use this for initialization
    void Start()
    {
        //CreateChunk(new WorldPos(0, 0, 1));
        CreateChunk(new WorldPos(0, 0, 0));
    }

    void Update()
    {
        
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.O))
            debugMode = debugMode != DebugMode.Octahedron ? DebugMode.Octahedron : DebugMode.None;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.G))
            debugMode = debugMode != DebugMode.Gradient ? DebugMode.Gradient : DebugMode.None;
    }

    void CreateChunk(WorldPos pos)
    {
        GameObject newChunk = Instantiate(chunk, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
        Chunk chunkScript = newChunk.GetComponent<Chunk>();
        chunkScript.chunkCoords = pos;
        chunkScript.world = GetComponent<World>();
    }

    public Chunk GetChunk(Vector3 pos)
    {
        return GameObject.Find("Chunk (0, 0, 0)").GetComponent<Chunk>();
    }
}
