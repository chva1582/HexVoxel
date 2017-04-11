//Represents points and visualizes some debug gizmos
//Attached to the Point Prefab
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
        //Only shows gizmos when the object is selected not the parents
        if (Selection.activeGameObject != transform.gameObject)
            return;

        Vector3 pos = transform.position;
        //Shows the Octahedron grid at the point and prints some info
        if (world.debugMode == DebugMode.Octahedron)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 5; j > i; j--)
                {
                    if (i % 2 == 1 || j != i + 1)
                        Gizmos.DrawLine(pos + GetTetra(i), pos + GetTetra(j));
                }
            }
            chunk.FaceBuilderCheck(chunk.PosToHex(pos).ToHexCell());
        }

        //Shows the Gradient Check with points shown (blue if above threshold red if below)
        if(world.debugMode == DebugMode.Gradient)
        {
            Vector3 gradient = chunk.GetNormalInterp(chunk.PosToHex(pos));//Procedural.Noise.noiseMethods[0][2](world.PosToHex(pos), Chunk.noiseScale).derivative*20 + new Vector3(0, Chunk.thresDropOff, 0);
            gradient = gradient.normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + gradient);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos - gradient);
            Gizmos.color = chunk.Land(world.PosToHex(pos)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos, .15f);
            Gizmos.color = chunk.Land(world.PosToHex(pos + gradient)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos + gradient, .05f);
            Gizmos.color = chunk.Land(world.PosToHex(pos - gradient)) ? Color.red : Color.blue;
            Gizmos.DrawSphere(pos - gradient, .05f);
        }
    }

    /// <summary>
    /// Relative point for Octahedron Index
    /// </summary>
    /// <param name="index">Octahedron Index</param>
    /// <returns>Relative Point</returns>
    Vector3 GetTetra(int index)
    {
        Vector3 vert = Chunk.tetraPoints[index];
        return vert;
    }
}