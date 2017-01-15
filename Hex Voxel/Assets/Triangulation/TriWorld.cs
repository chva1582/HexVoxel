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
            for (int x = Mathf.FloorToInt(-1f * (size / 2f)); x < Mathf.FloorToInt(size / 2f); x++)
            {
                for (int z = Mathf.FloorToInt(-1f * (size / 2f)); z < Mathf.FloorToInt(size / 2f); z++)
                {
                    GameObject newChunk = Instantiate(chunk, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
                    TriChunk chunkScript = newChunk.GetComponent<TriChunk>();
                    int w = TriChunk.chunkSize - 1;
                    chunkScript.posOffset = new Vector2(x * w, z * (w - 1));
                    chunkScript.world = GetComponent<TriWorld>();
                }
            }
        }
    }
}
