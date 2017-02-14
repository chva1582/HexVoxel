using UnityEditor;
using UnityEngine;

namespace Voxel
{
    public class PointData : MonoBehaviour
    {
        TriWorld world;
        TriChunk chunk;

        void Start()
        {
            world = GameObject.Find("World").GetComponent<TriWorld>();
        }

        void OnDrawGizmosSelected()
        {
            if (Selection.activeGameObject != transform.gameObject)
                return;
            Gizmos.color = Color.green;
            Vector3 pos = transform.position;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 5; j > i; j--)
                {
                    if (i % 2 == 1 || j != i + 1)
                        Gizmos.DrawLine(pos + GetTetra(i), pos + GetTetra(j));
                }
            }
            world.GetChunk(pos).FaceBuilderCheck(pos);
            WorldPos temp = world.GetChunk(pos).PosToHex(pos);
            print(world.GetChunk(pos).HexToPos(temp) + ", " + temp.x + ", " + temp.y + ", " + temp.z);
        }

        Vector3 GetTetra(int index)
        {
            Vector3 vert = TriChunk.tetraPoints[index];
            return vert;
        }
    }
}