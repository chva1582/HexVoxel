using UnityEngine;

public class World : MonoBehaviour
{
    public bool pointLoc;
    public bool show;
    public float size;
    public GameObject chunk;

    // Use this for initialization
    void Start()
    {
        CreateChunk(new WorldPos(0, 0, 1));
    }

    void CreateChunk(WorldPos pos)
    {
        GameObject newChunk = Instantiate(chunk, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
        Chunk chunkScript = newChunk.GetComponent<Chunk>();
        float wx = Chunk.chunkSize*Mathf.Sqrt(3)/1.5f;
        int wz = Chunk.chunkSize;
        int h = Chunk.chunkHeight;
        chunkScript.posOffset = new Vector3(pos.x * wx, pos.y * h, pos.z * wz);
        chunkScript.world = GetComponent<World>();
    }

    public Chunk GetChunk(Vector3 pos)
    {
        return GameObject.Find("Chunk(Clone)").GetComponent<Chunk>();
    }
}
