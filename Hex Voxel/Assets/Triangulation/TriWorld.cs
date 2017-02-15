using UnityEngine;

namespace Voxel
{
    public class TriWorld : MonoBehaviour
    {
        public bool pointLoc;
        public bool show;
        public float size;
        public GameObject chunk;

        // Use this for initialization
        void Start()
        {
            CreateChunk(new WorldPos(0, 0, 0));
        }

        void CreateChunk(WorldPos pos)
        {
            GameObject newChunk = Instantiate(chunk, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
            TriChunk chunkScript = newChunk.GetComponent<TriChunk>();
            float wx = TriChunk.chunkSize*Mathf.Sqrt(3)/1.5f;
            int wz = TriChunk.chunkSize;
            int h = TriChunk.chunkHeight;
            chunkScript.posOffset = new Vector3(pos.x * wx, pos.y * h, pos.z * wz);
            chunkScript.world = GetComponent<TriWorld>();
        }

        public TriChunk GetChunk(Vector3 pos)
        {
            return GameObject.Find("TriChunk(Clone)").GetComponent<TriChunk>();
        }
    }
}
