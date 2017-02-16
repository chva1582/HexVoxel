using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    #region Control Variables
    //General Info
    WorldPos chunkCoords;
    public bool update;
    public bool rendered;

    //Measurements
    public static int chunkSize = 64;
    public static int chunkHeight = 16;

    //Components
    public Vector3 posOffset = new Vector3();
    public World world;
    public GameObject dot;

    //Storage
    static bool[,,] hits = new bool[chunkSize, chunkHeight, chunkSize];

    //Noise Parameters
    static float noiseScale = 0.06f;
    public static float threshold = 14;
    public static float thresDropOff = 0;

    
    //Other Options
    public bool meshRecalculate;
    #endregion

    #region Calculated Lists

    public static Vector3[] tetraPoints = { new Vector3(0, 0, 0), new Vector3(Mathf.Sqrt(3), 0, 1),
        new Vector3(0, 0, 2), new Vector3(Mathf.Sqrt(3), 0, -1),
        new Vector3(Mathf.Sqrt(3)-(2*Mathf.Sqrt(3) / 3), -2 * Mathf.Sqrt(1-Mathf.Sqrt(3)/3), 1),
        new Vector3(2 * Mathf.Sqrt(3) / 3, 2 * Mathf.Sqrt(1 - Mathf.Sqrt(3) / 3), 0) };
#endregion

    void Start()
    {
        GenerateMesh(chunkSize);
    }

    #region On Draw
    /// <summary>
    /// Method called when object is selected
    /// </summary>
    void OnDrawGizmosSelected ()
    {
        for(int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkHeight; j++)
            {
                for (int k = 0; k < chunkSize - 1; k++)
                {
                    if (hits[i, j, k])
                    {
                        Vector3 vert = HexToPos(new WorldPos(i, j, k));
                        //vert = new Vector3(vert.x * Mathf.Sqrt(3) / 1.5f, vert.y * 2, vert.z);
                        Gizmos.color = Color.gray;
                        Gizmos.DrawSphere(vert, .2f);
                        if (world.show)
                        {
                            Vector3 dir = Procedural.Noise.noiseMethods[0][2](vert, noiseScale).derivative.normalized;
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawRay(vert, dir);
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Face Construction
    /// <summary>
    /// Creates mesh
    /// </summary>
    /// <param name="wid">Width of points to be generated</param>
    void GenerateMesh(int wid)
    {
        List<Vector3[]> verts = new List<Vector3[]>();
        for (int z = 0; z < wid; z++)
        {
            verts.Add(new Vector3[wid]);
            for (int x = 0; x < wid; x++)
            {
                Vector3 currentPoint = new Vector3(x + posOffset.x, posOffset.y, z + posOffset.z);

                int offset = z % 2;
                if (offset == 1)
                    currentPoint.x -= (1-tetraPoints[4].x/tetraPoints[5].x);

                float tempH = Mathf.Round(currentPoint.y);
                currentPoint.y += (2 * x + (offset == 1 ? 2 : 3)) % 3;
                currentPoint.y = (tempH - Mathf.Round(currentPoint.y)) / 3 + tempH;
                currentPoint.y *= (tetraPoints[5].y)*3/2;
                verts[z][x] = currentPoint;
            }
        }

        Vector3[] uVerts = new Vector3[wid * wid];
        int i = 0;
        foreach (Vector3[] v in verts)
        {
            v.CopyTo(uVerts, i * wid);
            i++;
        }
        MeshFromPoints(uVerts);
    }
    /// <summary>
    /// Creates a mesh for the chunk based on a set of points it was handed
    /// </summary>
    /// <param name="basePoints">Points to check</param>
    public void MeshFromPoints(Vector3[] basePoints)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        //Point Reading
        foreach (var basePoint in basePoints)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                Vector3 center = new Vector3(basePoint.x, basePoint.y + y * (tetraPoints[5].y) * 3 / 2, basePoint.z);
                if (Land(center) && GradientCheck(center))
                {
                    CreatePoint(center);
                }
            }
        }

        //Face Construction
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    FaceBuilder.Build(HexToPos(new WorldPos(x, y, z)), GetComponent<Chunk>(), ref verts, ref tris, ref normals);
                }
            }
        }

        //Mesh Procedure
        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        MeshCollider collider = gameObject.GetComponent<MeshCollider>();
        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();
        filter.mesh = mesh;
        collider.sharedMesh = mesh;
        if (meshRecalculate) { filter.mesh.RecalculateNormals(); }
    }
    #endregion

    #region Geometry
    void FaceBuilderMethod(Vector3 center, ref List<Vector3> verts, ref List<int> tris, ref List<Vector3> normals)
    {
        FaceBuilder.Build(center, GetComponent<Chunk>(), ref verts, ref tris, ref normals);
    }
    #endregion

    #region Update Mechanisms
    public bool UpdateChunk()
    {
        rendered = true;
        return true;
    }
    #endregion

    #region Checks
    /// <summary>
    /// Checks if a point is on the edge of a surface using IVT and gradients
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>Boolean</returns>
    bool GradientCheck(Vector3 point)
    {
        Vector3 normal = Procedural.Noise.noiseMethods[0][2](point, noiseScale).derivative.normalized;
        //normal = new Vector3(0, thresDropOff*20, 0);
        normal = normal.normalized * 2f;
        if (GetNoise(point + normal, noiseScale) > threshold - point.y * thresDropOff && GetNoise(point - normal, noiseScale) < threshold - point.y * thresDropOff)
            return true;
        return false;
    }

    /// <summary>
    /// Checks if the point is within a solid
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>Boolean</returns>
    bool Land(Vector3 point)
    {
        //print(GetNoise(point, noiseScale));
        return GetNoise(point, noiseScale) < threshold - point.y * thresDropOff;
    }

    /// <summary>
    /// Checks if a triangle faces the same direction as the noise
    /// </summary>
    /// <param name="center">Point to check</param>
    /// <param name="normal">Normal to check</param>
    /// <returns>Boolean</returns>
    public bool TriNormCheck(Vector3 center, Vector3 normal)
    {
        return 90 > Vector3.Angle(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative, normal);
    }

    /// <summary>
    /// Returns value of 3D noise at a point
    /// </summary>
    /// <param name="pos">Point to check</param>
    /// <param name="scale">Size of the waves</param>
    /// <returns>Value of the noise</returns>
    public static float GetNoise(Vector3 pos, float scale)
    {
        return Procedural.Noise.noiseMethods[0][2](pos, scale).value * 20 + 10;
    }

    public static Vector3 GetNormal(Vector3 pos)
    {
        return Procedural.Noise.noiseMethods[0][2](pos, noiseScale).derivative.normalized;
    }

    /// <summary>
    /// Finds if this point is in the checks array
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>Boolean</returns>
    public bool CheckHit(Vector3 point)
    {
        bool output;
        try { output = hits[PosToHex(point).x, PosToHex(point).y, PosToHex(point).z]; }
        catch { output = false; }
        return output;
    }
    #endregion

    #region Conversions
    /// <summary>
    /// Converts from World Position to Hex Coordinates
    /// </summary>
    /// <param name="point">World Position</param>
    /// <returns>Hex Coordinate</returns>
    public WorldPos PosToHex (Vector3 point)
    {
#warning There are fudge factors at work in these conversions
        point.y += .1f; 
        point.x -= .1f;
        point.y /= tetraPoints[5].y*3;
        point.x /= 2 * Mathf.Sqrt(3) / 3;
        point.x -= posOffset.x;
        point.y -= posOffset.y;
        point.z -= posOffset.z;
        WorldPos output = new WorldPos(Mathf.CeilToInt(point.x), Mathf.CeilToInt(point.y), (int)point.z);
            
        return output;
    }

    /// <summary>
    /// Converts from Hex Coordinate to World Position
    /// </summary>
    /// <param name="point">Hex Coordinate</param>
    /// <returns>World Position</returns>
    public Vector3 HexToPos (WorldPos point)
    {
        Vector3 currentPoint = new Vector3(point.x + posOffset.x, posOffset.y, point.z + posOffset.z);
        int offset = point.z % 2;
        if (offset == 1)
            currentPoint.x -= (1 - tetraPoints[4].x / tetraPoints[5].x);

        float tempH = Mathf.Round(currentPoint.y);
        currentPoint.y += (2 * point.x + (offset == 1 ? 2 : 3)) % 3;
        currentPoint.y = (tempH - Mathf.Round(currentPoint.y)) / 3 + tempH;
        currentPoint.y *= (tetraPoints[5].y) * 3 / 2;
        currentPoint.y += point.y * (tetraPoints[5].y) * 3 / 2;
        return new Vector3(currentPoint.x * Mathf.Sqrt(3) / 1.5f, currentPoint.y * 2, currentPoint.z);
    }
    #endregion

    #region Debug
    void CreatePoint(Vector3 location)
    {
        Vector3 warpedLocation = new Vector3(location.x * Mathf.Sqrt(3) / 1.5f, location.y * 2, location.z);
        if (world.pointLoc)
        {
            GameObject copy = Instantiate(dot, warpedLocation, new Quaternion(0, 0, 0, 0)) as GameObject;
            copy.transform.parent = gameObject.transform;
        }
    }

    public void FaceBuilderCheck(Vector3 center)
    {
        List<Vector3> vertTemp = new List<Vector3>();
        List<int> vertFail = new List<int>();
        List<int> vertSuccess = new List<int>();
        print(vertSuccess.Count);
        for (int i = 0; i < 6; i++)
        {
            Vector3 vert = center + tetraPoints[i];
            WorldPos hex = PosToHex(vert);
            //print(vert + ", " + hex.x + ", " + hex.y + ", " + hex.z + "(Check Point) " + i);
            if (CheckHit(vert))
            {
                vertTemp.Add(vert);
                vertSuccess.Add(i);
            }
            else
                vertFail.Add(i);
        }
        switch (vertTemp.Count)
        {
            case 6:
                print(center + "= " + "Octahedron");
                break;

            case 5:
                print(center + "= " + "Rectangular Prism");
                break;

            case 4:
                if (vertFail[0] == 4 && vertFail[1] == 5)
                    print(center + "= " + "Horizontal Square");
                else if (vertFail[0] == 2 && vertFail[1] == 3)
                    print(center + "= " + "Vertical Square");
                else if (vertFail[0] == 0 && vertFail[1] == 1)
                    print(center + "= " + "Vertical Square 2");
                else if (vertSuccess[2] == 4 && vertSuccess[3] == 5)
                    print(center + "= " + "Corner Tetrahedron");
                else if (vertFail[0] == 0 || vertFail[0] == 1)
                    print(center + "= " + "New Tetrahedron");
                else
                    print(center + "= " + "Tetrahedron");
                break;

            case 3:
                print(center + "= " + "Triangle");
                break;

            default:
                break;
        }
        if (CheckHit(center) && CheckHit(center + tetraPoints[1]) && CheckHit(center - tetraPoints[3] + tetraPoints[5]) && CheckHit(center + tetraPoints[2] + tetraPoints[5]))
            print(center + "= " + "Third Diagonal");
    }
    #endregion
}
