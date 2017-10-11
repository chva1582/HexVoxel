//A group of points of square size organized in octahedral coordinates
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    #region Control Variables
    //General Info
    public ChunkCoord chunkCoords;
    public bool update;
    public bool rendered = true;
    public bool cornersReady;
    bool uniform;
    bool fading;

    //Measurements
    public static int chunkSize = 8;

    //Components
    public HexWorldCoord HexOffset { get { return new HexWorldCoord(chunkCoords.x * chunkSize, chunkCoords.y * chunkSize, chunkCoords.z * chunkSize); } }
    public Vector3 PosOffset { get { return World.HexToPos(new HexWorldCoord(chunkCoords.x * chunkSize, chunkCoords.y * chunkSize, chunkCoords.z * chunkSize)); } }
    MeshFilter filter;
    MeshCollider coll;
    Mesh mesh;

    //Geometry
    ChunkBounds bounds = new ChunkBounds(0, 0, 0, 0, 0, 0);
    List<HexCell> verts = new List<HexCell>();
    List<int> tris = new List<int>();
    List<Vector3> normals = new List<Vector3>();
    List<int> vertSuccess = new List<int>();
    HexCell[] vertTemp = new HexCell[6];
    HashSet<HexCell> checkSet = new HashSet<HexCell>();
    Dictionary<HexCell, int> indexLookup = new Dictionary<HexCell, int>();

    //Public Objects
    public World world;
    public GameObject dot;

    //Storage
    bool pointsReady;
    bool[,,] vertexes = new bool[chunkSize, chunkSize, chunkSize];
    Chunk[] neighbors = new Chunk[6];
    bool[] neighborExists = new bool[6];

    //Edited Points
    public bool edited;
    public float[,,] editedValues = new float[chunkSize, chunkSize, chunkSize];
    public Normal[,,] editedNormals = new Normal[chunkSize, chunkSize, chunkSize];
    public NoiseData EditedData { get { return new NoiseData(editedValues, editedNormals); } }

    //Corners for Interpolation
    public float[,,] corners = new float[2, 2, 2];
    public Vector3[,,] cornerNormals = new Vector3[2, 2, 2];
    public bool[,,] cornerInitialized = new bool[2, 2, 2];
    
    //Other Options
    public bool meshRecalculate;
    #endregion

    #region Calculated Lists
    //Location of octahedron points in real space
    public static Vector3[] tetraPoints = { new Vector3(0, 0, 0), new Vector3(Mathf.Sqrt(3), 0, 1),
        new Vector3(0, 0, 2), new Vector3(Mathf.Sqrt(3), 0, -1),
        new Vector3(Mathf.Sqrt(3)-(2*Mathf.Sqrt(3) / 3), -2 * Mathf.Sqrt(1-Mathf.Sqrt(3)/3), 1),
        new Vector3(2 * Mathf.Sqrt(3) / 3, 2 * Mathf.Sqrt(1 - Mathf.Sqrt(3) / 3), 0) };

    //Location of octahedron points in hexagonal coordiantes
    public static HexCell[] hexPoints = {new HexCell(0,0,0), new HexCell(1,0,1), new HexCell(0,0,1),
        new HexCell(1,0,0),new HexCell(1,-1,1), new HexCell(0,1,0), new HexCell(-1,1,0), new HexCell(0,1,1)};

    //Shortcut Math
    public static float sqrt3 = Mathf.Sqrt(3);

    //Array of Neighboring Chunk's coordinates
    public static Vector3[] neighborChunkCoords = { new Vector3(-chunkSize * World.h, 0, chunkSize),
        new Vector3(chunkSize * World.h, 0, -chunkSize),
        new Vector3(-chunkSize * World.g, -chunkSize * World.f, 0),
        new Vector3(chunkSize * World.g, chunkSize * World.f, 0),
        new Vector3(0, 0, -2 * chunkSize), new Vector3(0, 0, 2 * chunkSize) };

    //Indices of the corners for each face of the chunk
    public static int[][] neighborChunkCorners = { new int[]{ 0,1,2,3}, new int[]{ 4,5,6,7}, new int[] {0,1,4,5},
        new int[] {2,3,6,7}, new int[] {0,2,4,6}, new int[] {1,3,5,7}};
    #endregion

    #region Start
    /// <summary>
    /// Initial Generation
    /// Setup variables, chunk is renamed, moves chunk, generates mesh
    /// </summary>
    void Start()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();
        mesh = new Mesh();
        gameObject.name = "Chunk (" + chunkCoords.x + ", " + chunkCoords.y + ", " + chunkCoords.z + ")";
        Serialization.LoadChunk(this);
        StartGeneration();
        if(world.fadeIn)
            StartCoroutine("FadeIn");
    }

    /// <summary>
    /// Generation on Reload from Chunk Pool
    /// Chunk is renamed, moved, and all geometric values are reset, generates mesh
    /// </summary>
    public void RebuildChunk()
    {
        gameObject.name = "Chunk (" + chunkCoords.x + ", " + chunkCoords.y + ", " + chunkCoords.z + ")";
        ResetValues();
        bounds.Reset();
        Serialization.LoadChunk(this);
        StartGeneration();
        if(world.fadeIn)
            StartCoroutine("FadeIn");
    }

    /// <summary>
    /// Generation on change in geometry
    /// Resets Geometric Values, generates mesh
    /// </summary>
    public void GeometricUpdateChunk()
    {
        ResetValues();
        StartGeneration();
    }

    /// <summary>
    /// Produces points and a mesh
    /// </summary>
    public void StartGeneration()
    {
        //Interpolation Values
        FindNeighbors();
        FindCorners();

        //Find values if near surface
        if (!uniform)
            GenerateMesh(new Vector3(chunkSize, chunkSize, chunkSize));

        //Mesh Finalizing Procedure
        mesh.RecalculateBounds();
        if (meshRecalculate) { mesh.RecalculateNormals(); }
        coll.sharedMesh = null;
        filter.mesh = mesh;
        coll.sharedMesh = mesh;
    }

    /// <summary>
    /// Resets all values in a Chunk
    /// </summary>
    void ResetValues()
    {
        uniform = false;
        fading = false;
        corners = new float[2, 2, 2];
        cornerInitialized = new bool[2, 2, 2];
        checkSet.Clear();
        indexLookup.Clear();
        verts.Clear();
        tris.Clear();
        normals.Clear();
        mesh.Clear();
        vertexes = new bool[chunkSize, chunkSize, chunkSize];
    }
#endregion

    #region On Draw
    /// <summary>
    /// Method called when object is selected
    /// </summary>
    void OnDrawGizmosSelected ()
    {
        for(int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                for (int k = 0; k < chunkSize - 1; k++)
                {
                    if (vertexes[i, j, k])
                    {
                        HexCoord vert = new HexCoord((byte)i, (byte)j, (byte)k);
                        Gizmos.color = Color.gray;
                        Gizmos.DrawSphere(HexToPos(vert.ToHexCell()), .2f);
                        if (world.showNormals)
                        {
                            Vector3 dir = GetNormal(vert);
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawRay(HexToPos(vert.ToHexCell()), dir);
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Face Construction
    /// <summary>
    /// Finds the neighboring Chunks if they exist. Used for interpolation and corner sharing
    /// </summary>
    void FindNeighbors()
    {
        for (int i = 0; i < 6; i++)
        {
            try { neighbors[i] = world.GetChunk(PosOffset + neighborChunkCoords[i]); }
            catch { }
        }
    }

    /// <summary>
    /// Finds the value of the corners of the Chunk and saves the for use in the interpolation
    /// </summary>
    void FindCorners()
    {
        for (int i = 0; i < 6; i++)
        {
            try { neighborExists[i] = neighbors[i].cornersReady; }
            catch { neighborExists[i] = false; }
            if (neighborExists[i])
            {
                for (int j = 0; j < 4; j++)
                {
                    int cornerIndex = neighborChunkCorners[i][j];
                    int x = cornerIndex >= 4 ? 1 : 0;
                    int y = cornerIndex / 2 % 2 == 1 ? 1 : 0;
                    int z = cornerIndex % 2 == 1 ? 1 : 0;
                    corners[x, y, z] = neighbors[i].corners[i < 2 ? (x == 1 ? 0 : 1) : x, (i == 2 || i == 3) ? (y == 1 ? 0 : 1) : y, i > 3 ? (z == 1 ? 0 : 1) : z];
                    cornerNormals[x, y, z] = neighbors[i].cornerNormals[i < 2 ? (x == 1 ? 0 : 1) : x, (i == 2 || i == 3) ? (y == 1 ? 0 : 1) : y, i > 3 ? (z == 1 ? 0 : 1) : z];
                    cornerInitialized[x, y, z] = true;
                }
            }
        }
        for (int i = 0; i < 8; i++)
        {
            int x = i < 4 ? 1 : 0;
            int y = i / 2 % 2 == 1 ? 1 : 0;
            int z = i % 2 == 1 ? 1 : 0;
            if (!cornerInitialized[x, y, z])
            {
                corners[x, y, z] = GetNoise(new HexCoord(chunkSize * x, chunkSize * y, chunkSize * z));
                cornerNormals[x, y, z] = GetNormal(new HexCoord(chunkSize * x, chunkSize * y, chunkSize * z));
            }
        }
        cornersReady = true;
        CheckUniformity();
    }
    
    /// <summary>
    /// Creates mesh
    /// </summary>
    /// <param name="size">Vector3 of the Chunk size to be generated</param>
    void GenerateMesh(Vector3 size)
    {
        VertexWrite(size);
        FaceConstruction(size);
        MeshProcedure();
    }

    /// <summary>
    /// Writes a boolean array of which points are active
    /// </summary>
    /// <param name="size">Vector3 of the Chunk size to be generated</param>
    void VertexWrite(Vector3 size)
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int k = 0; k < size.z; k++)
                {
                    HexCell center = new HexCell(i, j, k);
                    HexWorldCoord shiftedCenter = HexToWorldHex(center.ToHexCoord());
                    if (world.GradientCheck(shiftedCenter))
                    {
                        vertexes[i, j, k] = true;
                        if (world.pointMode == PointMode.Gradient) { CreatePoint(center.ToHexCoord()); }
                    }
                    if (world.pointMode == PointMode.All) { CreatePoint(center.ToHexCoord()); }
                }
            }
        }
    }

    /// <summary>
    /// Creates the faces in the Chunk 
    /// </summary>
    /// <param name="size">Vector3 of the Chunk size to be generated</param>
    void FaceConstruction(Vector3 size)
    {
        if(!bounds.Initialized)
            CalculateBounds();
        for (int i = bounds.xL; i < bounds.xH; i++)
        {
            for (int j = bounds.yL; j < bounds.yH; j++)
            {
                for (int k = bounds.zL; k < bounds.zH; k++)
                {
                    Build(new HexCell(i, j, k));
                }
            }
        }
    }

    /// <summary>
    /// Builds the faces at a single Octahedron
    /// </summary>
    /// <param name="center">0-point of the octahedron to be built</param>
    void Build(HexCell center)
    {
        vertSuccess.Clear();
        GetHitList(center);
        bool[] hitList = new bool[6];
        foreach (int success in vertSuccess)
            hitList[success] = true;

        int[] tempTriArray;
        HexCell[] tempVertArray;

        tempTriArray = World.triLookup[World.boolArrayToInt(hitList)];
        tempVertArray = World.vertLookup[World.boolArrayToInt(hitList)];

        if (tempTriArray == null)
            tempTriArray = new int[0];
        if (tempVertArray == null)
            tempVertArray = new HexCell[0];

        
        if (world.flatRender)
        {
            for (int i = 0; i < tempTriArray.Length/3; i++)
            {
                int tri1 = tempTriArray[3 * i];
                int tri2 = tempTriArray[3 * i + 1];
                int tri3 = tempTriArray[3 * i + 2];

                Vector3 left = HexToPos(center + tempVertArray[tri2]) - HexToPos(center + tempVertArray[tri1]);
                Vector3 right = HexToPos(center + tempVertArray[tri3]) - HexToPos(center + tempVertArray[tri1]);

                if(!TriNormCheck(HexToPos(center), Vector3.Cross(left, right)))
                {
                    tempTriArray[3 * i] = tri3;
                    tempTriArray[3 * i + 2] = tri1;
                }
            }
            List<HexCell> temptempVert = new List<HexCell>();
            foreach (HexCell vert in tempVertArray)
            {
                temptempVert.Add(vert + center);
                print(vert + center);
            }

            foreach (int tri in tempTriArray)
                tris.Add(tri + verts.Count);

            verts.AddRange(temptempVert);
            print(string.Empty);
        }
        else
        {
            foreach (HexCell vert in tempVertArray)
            {
                if (checkSet.Add(vert + center))
                {
                    indexLookup.Add(vert + center, verts.Count);
                    verts.Add(vert + center);
                }
            }

            foreach (int tri in tempTriArray)
            {
                int index;
                indexLookup.TryGetValue(tempVertArray[tri] + center, out index);
                tris.Add(index);
            }
        }

        BuildThirdSlant(center);
    }

    /// <summary>
    /// Find a list of the hits for the other points of the octahedron
    /// </summary>
    /// <param name="center">0-point of the octahedron to be built</param>
    void GetHitList(HexCell center)
    {
        for (int i = 0; i < 6; i++)
        {
            HexCell vert = (center + hexPoints[i]);
            if (CheckHit(vert))
                vertSuccess.Add(i);
        }
    }

    void CalculateBounds()
    {
        bounds.xL = neighborExists[0] ? -1 : 1;
        bounds.xH = (neighborExists[1] ? chunkSize + 1 : chunkSize - 1);
        bounds.yL = neighborExists[2] ? -1 : 1;
        bounds.yH = (neighborExists[3] ? chunkSize + 1 : chunkSize - 1);
        bounds.zL = neighborExists[4] ? -1 : 0;
        bounds.zH = (neighborExists[5] ? chunkSize : chunkSize - 1);
        bounds.Initialized = true;
    }

    /// <summary>
    /// Construct the Third Slant of each Octahedron if necessary
    /// The Third Slant is the only missing face from the Octahedron scheme
    /// </summary>
    /// <param name="center">0-point of the octahedron to be built</param>
    void BuildThirdSlant(HexCell center)
    {
        if (CheckHit(center) && CheckHit(center + hexPoints[1]) && 
            CheckHit(center - hexPoints[3] + hexPoints[5]) && 
            CheckHit(center + hexPoints[2] + hexPoints[5]) && World.thirdDiagonalActive)
        {
            int vertCount = verts.Count;

            vertTemp[0] = (center);
            vertTemp[1] = (center - hexPoints[3] + hexPoints[5]);
            vertTemp[2] = (center + hexPoints[2] + hexPoints[5]);
            vertTemp[3] = (center);
            vertTemp[4] = (center + hexPoints[2] + hexPoints[5]);
            vertTemp[5] = (center + hexPoints[1]);
            if (world.flatRender)
            {
                for (int i = 0; i < 6; i++)
                {
                    verts.Add(vertTemp[i]);
                    tris.Add(vertCount + i);
                }
                for (int i = 0; i < 6; i++)
                {
                    verts.Add(vertTemp[5 - i]);
                    tris.Add(vertCount + 6 + i);
                }

            }
            else
            {
                foreach (HexCell vert in vertTemp)
                {
                    if (checkSet.Add(vert))
                    {
                        indexLookup.Add(vert, verts.Count);
                        verts.Add(vert);
                    }
                }

                for (int index = 0; index < 6; index++)
                {
                    int i;
                    indexLookup.TryGetValue(vertTemp[index], out i);
                    tris.Add(i);
                }
            }
        }
    }

    /// <summary>
    /// Send mesh data to the mesh and apply after affects
    /// </summary>
    void MeshProcedure()
    {
        List<Vector3> posVerts = new List<Vector3>();
        foreach (HexCell hexCell in verts)
        {
            HexCoord hex = hexCell.ToHexCoord();
            HexWorldCoord point = HexToWorldHex(hex);
            normals.Add(GetNormal(hex));
            Vector3 offset = new Vector3();
            Vector3 smooth = new Vector3();
            if (world.smoothLand)
            {
                Vector3 norm = GetNormal(hex);
                norm = norm.normalized * sqrt3 / 2;
                float A = GetNoise(PosToHex(World.HexToPos(point) + norm));
                float B = GetNoise(PosToHex(World.HexToPos(point) - norm));
                float T = 0;
                smooth = norm.normalized * ((A + B) / 2 - T) / ((A - B) / 2) * -sqrt3 / 2;
            }
            if (world.offsetLand)
            {
                offset = .4f * World.GetNormalNonTerrain((HexToPos(hexCell) + smooth) * 27);
            }  
            posVerts.Add(HexToPos(hexCell) + offset + smooth);
        }
        mesh.SetVertices(posVerts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
    }
    #endregion

    #region Update Mechanisms
    /// <summary>
    /// Request the update of a Chunk
    /// </summary>
    /// <returns>Success of update</returns>
    public bool UpdateChunk()
    {
        rendered = true;
        return true;
    }

    public void EditPointValue(HexCell cell, float change)
    {
        update = false;
        try
        {
            editedValues[cell.x, cell.y, cell.z] += change;
        }
        catch 
        {
            
        }
        edited = true;
    }

    public void EditPointNormal(HexCell cell, Vector3 change)
    {
        update = false;
        try
        {
            editedNormals[cell.x, cell.y, cell.z] += change;
        }
        catch
        {
            
        }
        edited = true;
    }

    float GetNoise(HexCoord coord)
    {
        HexCell cell = coord.ToHexCell();
        try
        {
            return world.GetNoise(HexToWorldHex(coord)) + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return world.GetNoise(HexToWorldHex(coord)); }
    }

    Vector3 GetNormal(HexCoord coord)
    {
        HexCell cell = coord.ToHexCell();
        try
        {
            return world.GetNormal(HexToWorldHex(coord)) + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return world.GetNormal(HexToWorldHex(coord)); }
    }
    #endregion

    #region Fade
    IEnumerator FadeIn()
    {
        Material mat = GetComponent<Renderer>().material;
        while (mat.color.a < 1)
        {
            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, mat.color.a + Time.deltaTime);
            yield return null;
        }
        
        if (world.flatRender)
            mat = Resources.Load("Terrain") as Material;
        else
            mat = Resources.Load("TerrainBackCullOff") as Material;
        GetComponent<Renderer>().material = mat;
    }

    public void FadeOutCall()
    {
        if (!fading)
            StartCoroutine("FadeOut");
    }

    IEnumerator FadeOut()
    {
        fading = true;
        Material mat = GetComponent<Renderer>().material;
        mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 1);
        while (mat.color.a > 0)
        {
            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, mat.color.a - Time.deltaTime);
            yield return null;
        }
        world.DestroyChunk(chunkCoords);
    }
    #endregion

    #region Checks
    /// <summary>
    /// Checks if a triangle faces the same direction as the noise
    /// </summary>
    /// <param name="center">Point to check</param>
    /// <param name="normal">Normal to check</param>
    /// <returns>Boolean</returns>
    public bool TriNormCheck(Vector3 center, Vector3 normal)
    {
        return 90 > Vector3.Angle(GetNormal(PosToHex(center)), normal);
    }

    public float GetInterp(HexCoord pos)
    {
        HexWorldCoord low = HexOffset;
        HexWorldCoord high = HexOffset + new HexCoord(chunkSize, chunkSize, chunkSize);

        float xD = (pos.x - low.x) / (high.x - low.x);
        float yD = (pos.y - low.y) / (high.y - low.y);
        float zD = (pos.z - low.z) / (high.z - low.z);

        float c00 = Mathf.Lerp(corners[0, 0, 0], corners[1, 0, 0], xD);
        float c01 = Mathf.Lerp(corners[0, 0, 1], corners[1, 0, 1], xD);
        float c10 = Mathf.Lerp(corners[0, 1, 0], corners[1, 1, 0], xD);
        float c11 = Mathf.Lerp(corners[0, 1, 1], corners[1, 1, 1], xD);

        float c0 = Mathf.Lerp(c00, c10, yD);
        float c1 = Mathf.Lerp(c01, c11, yD);

        float c = Mathf.Lerp(c0, c1, zD);
        return c;
    }

    public Vector3 GetNormalInterp(HexCoord pos)
    {
        HexWorldCoord low = HexOffset;
        HexWorldCoord high = HexOffset + new HexCoord(chunkSize, chunkSize, chunkSize);

        float xD = (pos.x - low.x) / (high.x - low.x);
        float yD = (pos.y - low.y) / (high.y - low.y);
        float zD = (pos.z - low.z) / (high.z - low.z);

        Vector3 c00 = Vector3.Lerp(cornerNormals[0, 0, 0], cornerNormals[1, 0, 0], xD);
        Vector3 c01 = Vector3.Lerp(cornerNormals[0, 0, 1], cornerNormals[1, 0, 1], xD);
        Vector3 c10 = Vector3.Lerp(cornerNormals[0, 1, 0], cornerNormals[1, 1, 0], xD);
        Vector3 c11 = Vector3.Lerp(cornerNormals[0, 1, 1], cornerNormals[1, 1, 1], xD);

        Vector3 c0 = Vector3.Lerp(c00, c10, yD);
        Vector3 c1 = Vector3.Lerp(c01, c11, yD);

        Vector3 c = Vector3.Lerp(c0, c1, zD);
        return c;
    }

    /// <summary>
    /// Finds if this point is in the checks array
    /// </summary>
    /// <param name="point">hex point to check</param>
    /// <returns>Boolean</returns>
    public bool CheckHit(HexCell point)
    {
        bool output;
        if (point.x < chunkSize && point.x > -1 && point.y < chunkSize && point.y > -1 && point.z < chunkSize && point.z > -1)
            output = vertexes[(int)(point.x + .5f), (int)(point.y + .5f), (int)(point.z + .5f)];
        else
        {
            try
            {
                Vector3 truePos = HexToPos(point);
                Chunk neighbor = world.GetChunk(truePos);
                HexCoord hex = neighbor.PosToHex(truePos);
                output = neighbor.vertexes[(int)(hex.x + .1f), (int)(hex.y + .1f), (int)(hex.z + .1f)];
            }
            catch {  output = world.CheckHit(HexToPos(point)); }
        }
        
        return output;
    }

    void CheckUniformity()
    {
        bool allLow = true;
        bool allHigh = true;
        foreach (var corner in corners)
        {
            if (corner - World.threshold < 1)
                allHigh = false;
            if (corner - World.threshold > -1)
                allLow = false;
        }
        if (allHigh || allLow)
            uniform = true;
    }
    #endregion

    #region Conversions
    /// <summary>
    /// Converts from World Position to Hex Coordinates
    /// </summary>
    /// <param name="point">World Position</param>
    /// <returns>Hex Coordinate</returns>
    public HexCoord PosToHex (Vector3 point)
    {
        point.x -= PosOffset.x;
        point.y -= PosOffset.y;
        point.z -= PosOffset.z;
        return World.PosToHex(point).ToHexCoord();
    }

    /// <summary>
    /// Converts from Hex Coordinate to World Position
    /// </summary>
    /// <param name="point">Hex Coordinate</param>
    /// <returns>World Position</returns>
    public Vector3 HexToPos (HexCell point)
    {
        Vector3 output = new Vector3();
        output = World.HexToPos(point.ToHexCoord());
        output.x += PosOffset.x;
        output.y += PosOffset.y;
        output.z += PosOffset.z;
        return output;
    }

    public HexWorldCoord HexToWorldHex(HexCoord point)
    {
        return HexOffset + point;
    }
    #endregion

    #region Debug
    void CreatePoint(HexCoord location, bool forced = false)
    {
        HexCell posLoc = new HexCell((byte)Mathf.RoundToInt(location.x), (byte)Mathf.RoundToInt(location.y), (byte)Mathf.RoundToInt(location.z));
        Vector3 warpedLocation = HexToPos(posLoc);
        if (world.pointLoc || forced)
        {
            GameObject copy = Instantiate(dot, warpedLocation, new Quaternion(0, 0, 0, 0)) as GameObject;
            copy.transform.parent = gameObject.transform;
        }
    }

    public void FaceBuilderCheck(HexCell center)
    {
        List<HexCell> vertTemp = new List<HexCell>();
        List<int> vertFail = new List<int>();
        List<int> vertSuccess = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            HexCell vert = center + hexPoints[i];
            print(vert + "(Check Point) " + i + ", " + CheckHit(vert));
            if (CheckHit(vert))
            {
                vertTemp.Add(vert);
                vertSuccess.Add(i);
            }
            else
                vertFail.Add(i);
        }
        print(vertSuccess.Count);
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
        if (CheckHit(center) && CheckHit(center + hexPoints[1]) && CheckHit(center - hexPoints[3] + hexPoints[5]) && CheckHit(center + hexPoints[2] + hexPoints[5]))
            print(center + "= " + "Third Diagonal");
    }
    #endregion

    #region Structs
    public struct ChunkBounds
    {
        public int xL, xH, yL, yH, zL, zH;
        bool initialized;

        public bool Initialized
        {
            get
            {
                return initialized;
            }

            set
            {
                initialized = value;
            }
        }

        public ChunkBounds(int XL, int XH, int YL, int YH, int ZL, int ZH)
        {
            xL = XL;
            xH = XH;
            yL = YL;
            yH = YH;
            zL = ZL;
            zH = ZH;
            initialized = false;
        }

        public void Reset()
        {
            xL = 0;
            xH = 0;
            yL = 0;
            yH = 0;
            zL = 0;
            zH = 0;
            Initialized = false;
        }
    }
    #endregion
}
