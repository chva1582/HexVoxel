using UnityEditor;
using UnityEngine;

public class PointData : MonoBehaviour
{
    World world;
    Chunk chunk;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        chunk = world.GetChunk(transform.position);
    }

    void OnDrawGizmosSelected()
    {
        if (Selection.activeGameObject != transform.gameObject)
            return;
        Vector3 pos = transform.position;
        if (world.debugMode == DebugMode.Octahedron)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 5; j > i; j--)
                {
                    if (i % 2 == 1 || j != i + 1)
                        Gizmos.DrawLine(pos + GetTetra(i), pos + GetTetra(j));
                }
            }
            chunk.FaceBuilderCheck(chunk.PosToHex(pos).ToVector3());
        }
        if(world.debugMode == DebugMode.Gradient)
        {
            Vector3 gradient = Procedural.Noise.noiseMethods[0][2](chunk.PosToHex(pos).ToVector3(), Chunk.noiseScale).derivative.normalized + new Vector3(0, Chunk.thresDropOff, 0);
            gradient = gradient.normalized * 2;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + gradient);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos - gradient);
            Gizmos.color = chunk.Land(chunk.PosToHexUncut(pos)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos, .15f);
            Gizmos.color = chunk.Land(chunk.PosToHexUncut(pos + gradient)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos + gradient, .05f);
            Gizmos.color = chunk.Land(chunk.PosToHexUncut(pos - gradient)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos - gradient, .05f);
            print(chunk.GradientCheck(chunk.PosToHexUncut(pos)));
        }
        WorldPos temp = chunk.PosToHex(pos);
        //print(chunk.HexToPos(temp) + ", " + temp.x + ", " + temp.y + ", " + temp.z);
    }

    Vector3 GetTetra(int index)
    {
        Vector3 vert = Chunk.tetraPoints[index];
        return vert;
    }
}