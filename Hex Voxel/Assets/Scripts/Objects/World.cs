//The overarching GameObject that holds world settings
//Applied to the World Object
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebugMode { None, Octahedron, Gradient }
public enum PointMode { None, Gradient, All}

public class World : MonoBehaviour
{
    #region Static Variables
    //Geometric Booleans
    public static bool sixPointActive = true, sixFaceCancel;
    public static bool fivePointActive = true, fiveFaceCancel;
    public static bool fourHoriSquareActive = true, fourHoriFaceReverse;
    public static bool fourVertSquareActive = true, fourVertFaceReverse;
    public static bool fourTetraActive = true, fourTetraFaceCancel;
    public static bool threePointActive = true, threeFaceReverse;
    public static bool thirdDiagonalActive = true, thirdDiagonalFaceReverse;
    
    //Noise Parameters
    public static float noiseScale = 0.03f;
    public static float threshold = 0f;
    public static float thresDropOff = 1f;
    public static float islandDropOff = 1.001f;

    public static readonly float f = 2f * Mathf.Sqrt(1 - (1 / Mathf.Sqrt(3)));
    public static readonly float g = (2f / 3f) * Mathf.Sqrt(3);
    public static readonly float h = Mathf.Sqrt(3);

    static Vector3[] p2H = { new Vector3(1f / h, ((-1f) * g) / (f * h), 0),
        new Vector3(0, 1f / f, 0), new Vector3(1f / (2f * h), ((-1f) * g) / (2f * f * h), 1f / 2f) };

    static Vector3[] h2P = { new Vector3(h, g, 0), new Vector3(0, f, 0), new Vector3(-1, 0, 2) };

    public static List<HexCell[]> vertLookup = new List<HexCell[]>();
    public static List<int[]> triLookup = new List<int[]>();
    #endregion

    #region Object Variables
    public string worldName;
    public bool pointLoc;
    public bool showNormals;
    public bool areaLoad;
    public bool loadOldChunk;
    public bool removeChunks;
    public static bool island = true;
    int heightBoundary = -15;
    public bool offsetLand;
    public bool smoothLand;
    public bool flatRender;
    public bool fadeIn;
    public bool reloadRenderLists;
    public RenderDistanceName renderDistance;
    public GameObject chunk;

    public Dictionary<ChunkCoord, Chunk> chunks = new Dictionary<ChunkCoord, Chunk>();
    public Queue<GameObject> chunkPool = new Queue<GameObject>();

    public DebugMode debugMode = DebugMode.None;
    public PointMode pointMode;
    #endregion

    #region Variable Setup
    void LookupTableConstruction()
    {
        string vertStringFull = string.Empty;
        string triStringFull = string.Empty;
        if (flatRender)
        {
            vertStringFull = (Resources.Load("FlatVerticesDictionary") as TextAsset).text;
            triStringFull = (Resources.Load("FlatTrianglesDictionary") as TextAsset).text;
        }
        else
        {
            vertStringFull = (Resources.Load("CloudVerticesDictionary") as TextAsset).text;
            triStringFull = (Resources.Load("CloudTrianglesDictionary") as TextAsset).text;
        }

        string[] arrayItemsV = vertStringFull.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        string[] arrayItemsT = triStringFull.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        foreach (string item in arrayItemsV)
        {
            string vertsString = item;
            List<HexCell> vertList = new List<HexCell>();
            foreach (string vert in vertsString.Split(new char[] { '.' }))
            {
                if (vert == "")
                    continue;
                HexCell extractedVert = new HexCell();
                extractedVert.X = sbyte.Parse(vert.Split(new char[] { ',' })[0]);
                extractedVert.Y = sbyte.Parse(vert.Split(new char[] { ',' })[1]);
                extractedVert.Z = sbyte.Parse(vert.Split(new char[] { ',' })[2]);
                vertList.Add(extractedVert);
            }
            vertLookup.Add(vertList.ToArray());
        }
        foreach (string item in arrayItemsT)
        {
            string trisString = item;
            
            List<int> triList = new List<int>();
            foreach (string tri in trisString.Split(new char[] { '.' }))
            {
                if (tri == "")
                    continue;
                triList.Add(int.Parse(tri));
            }
            triLookup.Add(triList.ToArray());
        }

    }
    #endregion

    #region Start, Update
    // Use this for initialization
    void Start()
    {
        if (!areaLoad && loadOldChunk)
        {
            CreateChunk(new ChunkCoord(-2, 0, 3));
            //CreateChunk(new ChunkCoord(3, -2, 3));
        }
        LookupTableConstruction();
    }

    void Update()
    {
        
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.O))
            debugMode = debugMode != DebugMode.Octahedron ? DebugMode.Octahedron : DebugMode.None;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.G))
            debugMode = debugMode != DebugMode.Gradient ? DebugMode.Gradient : DebugMode.None;
        //if(Input.GetKeyDown(KeyCode.Return))
        //    CreateChunk(new ChunkCoord(2, 0, -1));
    }
    #endregion

    #region Chunk Control
    /// <summary>
    /// Instantiates a new Chunk and sets it up
    /// </summary>
    /// <param name="pos">Chunk Coords of the Chunk to be created</param>
    public void CreateChunk(ChunkCoord pos)
    {
        if (chunks.ContainsKey(pos))//This is kind of cheating this should have already been checked but some were getting through
            return;
        GameObject newChunk;
        if (chunkPool.Count > 0)
        {
            newChunk = chunkPool.Dequeue();
            newChunk.SetActive(true);
            Chunk chunkScript = newChunk.GetComponent<Chunk>();
            chunkScript.chunkCoords = pos;
            chunkScript.world = GetComponent<World>();
            chunkScript.RebuildChunk();
            chunks.Add(chunkScript.chunkCoords, chunkScript);
        }
        else
        {
            newChunk = Instantiate(chunk, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;

            Chunk chunkScript = newChunk.GetComponent<Chunk>();
            chunkScript.meshRecalculate = flatRender;
            chunkScript.chunkCoords = pos;
            chunkScript.world = GetComponent<World>();

            if (fadeIn)
            {
                if (flatRender)
                    newChunk.GetComponent<Renderer>().material = Resources.Load("TerrainFade") as Material;
                else
                    newChunk.GetComponent<Renderer>().material = Resources.Load("TerrainBackCullOffFade") as Material;
            }
            else
            {
                if (flatRender)
                    newChunk.GetComponent<Renderer>().material = Resources.Load("Terrain") as Material;
                else
                    newChunk.GetComponent<Renderer>().material = Resources.Load("TerrainBackCullOff") as Material;
            }

            chunks.Add(chunkScript.chunkCoords, chunkScript);
        }
    }

    /// <summary>
    /// Find the Chunk at a current position
    /// </summary>
    /// <param name="pos">True Position of test point</param>
    /// <returns>Chunk script</returns>
    public Chunk GetChunk(Vector3 pos)
    {
        ChunkCoord chunkCoord = PosToChunk(pos);
        Chunk output;
        if (chunks.TryGetValue(chunkCoord, out output))
            return output;
        else
        {
            return null;
        }
            
    }

    /// <summary>
    /// Find the Chunk at a current position
    /// </summary>
    /// <param name="pos">True Position of test point</param>
    /// <returns>Chunk script</returns>
    public Chunk GetChunk(ChunkCoord chunkCoord)
    {
        Chunk output;
        if (chunks.TryGetValue(chunkCoord, out output))
            return output;
        else
        {
            return null;
        }

    }

    /// <summary>
    /// Eliminates a Chunk at given Chunk Coordiantes
    /// </summary>
    /// <param name="chunkCoord">Chunk Coordinates</param>
    public void DestroyChunk(ChunkCoord chunkCoord)
    {
        Chunk targetChunk = GetChunk(ChunkToPos(chunkCoord));
        if (targetChunk == null)
            print(targetChunk.chunkCoords);
        if(targetChunk.edited)
            Serialization.SaveChunk(targetChunk);
        targetChunk.gameObject.SetActive(false);
        chunkPool.Enqueue(targetChunk.gameObject);
        chunks.Remove(chunkCoord);
    }
    #endregion

    #region Checks
    /// <summary>
    /// Finds if a point is active on a global scale
    /// </summary>
    /// <param name="pos">True position of test point</param>
    /// <returns>Boolean</returns>
    public bool CheckHit(Vector3 pos)
    {
        try
        {
            Chunk chunk = GetChunk(pos);
            return chunk.CheckHit(chunk.PosToHex(pos).ToHexCell());
        }
        catch
        {
            HexWorldCell hex = PosToHex(pos).ToHexWorldCell();
            Vector3 gradient = GetNormal(hex.ToHexWorldCoord());
            gradient = gradient.normalized;
            if (!Land(PosToHex(HexToPos(hex.ToHexWorldCoord()) + gradient)) && Land(PosToHex(HexToPos(hex.ToHexWorldCoord()) - gradient)))
                return true;
            Vector3 gradientHigh = GetNormal(hex + PosToHex(gradient * 0.5f));
            Vector3 gradientLow = GetNormal(hex - PosToHex(gradient * 0.5f));
            gradientHigh = gradientHigh.normalized * Chunk.sqrt3 * 0.25f;
            gradientLow = gradientLow.normalized * Chunk.sqrt3 * 0.25f;
            if (!Land(hex + PosToHex(gradient * 0.5f) + PosToHex(gradientHigh)) && Land(hex - PosToHex(gradient * 0.5f) + PosToHex(gradientLow)))
                return true;
            return false;
        }
    }

    public bool Land(HexWorldCoord point, int forcedLocation = 0)
    {
        switch (forcedLocation)
        {
            case -1:
                return GetNoiseBelow(point) < threshold;
            case 1:
                return GetNoiseAbove(point) < threshold;
            default:
                return GetNoise(point) < threshold;
        }
    }

    public float GetNoise(HexWorldCoord hex)
    {
        Vector3 pos = HexToPos(hex);
        
        if (pos.y > heightBoundary)
            return GetNoiseAbove(hex);
        else
            return GetNoiseBelow(hex);
    }

    float GetNoiseAbove(HexWorldCoord hex)
    {
        Vector3 pos = HexToPos(hex);
        float noise = Procedural.Noise.noiseMethods[2][2](hex.ToVector3(), noiseScale, worldName.GetHashCode()).value * 20 + hex.y * thresDropOff;
        if (island) { noise += 100 * Mathf.Pow(islandDropOff, (Mathf.Pow(pos.x, 2) + Mathf.Pow(pos.z, 2)) - 10386); }
        return noise;
    }

    float GetNoiseBelow(HexWorldCoord hex)
    {
        Vector3 pos = HexToPos(hex);
        Vector3 basePos = new Vector3(pos.x, heightBoundary, pos.z);
        HexWorldCoord baseHex = PosToHex(basePos);
        float noise = Procedural.Noise.noiseMethods[2][2](baseHex.ToVector3(), noiseScale, worldName.GetHashCode()).value * 20 + baseHex.y * thresDropOff;
        if (island) { noise += 100 * Mathf.Pow(islandDropOff, (Mathf.Pow(basePos.x, 2) + Mathf.Pow(basePos.z, 2)) - 10386); }
        noise += 5 * (Mathf.Pow(-pos.y + heightBoundary, 0.35f));
        return noise;
    }

    public Vector3 GetNormal(HexWorldCoord hex, int forcedLocation = 0)
    {
        switch (forcedLocation)
        {
            case -1:
                return GetNormalBelow(hex);
            case 1:
                return GetNormalAbove(hex);
            default:
                Vector3 pos = HexToPos(hex);
                if (pos.y > heightBoundary)
                    return GetNormalAbove(hex);
                else
                    return GetNormalBelow(hex);
        }
    }

    Vector3 GetNormalAbove(HexWorldCoord hex)
    {
        Vector3 pos = HexToPos(hex);
        Vector3 gradient;
        gradient = Procedural.Noise.noiseMethods[2][2](hex.ToVector3(), noiseScale, worldName.GetHashCode()).derivative * 20 + new Vector3(0, thresDropOff, 0);
        if (island) { gradient += 100 * new Vector3(pos.x * Mathf.Log(islandDropOff) * Mathf.Pow(islandDropOff, (Mathf.Pow(pos.x, 2) + Mathf.Pow(pos.z, 2)) - 10386), 0, pos.z * Mathf.Log(islandDropOff) * Mathf.Pow(islandDropOff, (Mathf.Pow(pos.x, 2) + Mathf.Pow(pos.z, 2)) - 10386)); }
        return gradient;
    }

    Vector3 GetNormalBelow(HexWorldCoord hex)
    {
        Vector3 pos = HexToPos(hex);
        Vector3 gradient;
        Vector3 basePos = new Vector3(pos.x, heightBoundary, pos.z);
        HexWorldCoord baseHex = PosToHex(basePos);
        gradient = Procedural.Noise.noiseMethods[2][2](baseHex.ToVector3(), noiseScale, worldName.GetHashCode()).derivative * 20 + new Vector3(0, thresDropOff, 0);
        if (island) { gradient += 100 * new Vector3(basePos.x * Mathf.Log(islandDropOff) * Mathf.Pow(islandDropOff, (Mathf.Pow(basePos.x, 2) + Mathf.Pow(basePos.z, 2)) - 10386), 0, pos.z * Mathf.Log(islandDropOff) * Mathf.Pow(islandDropOff, (Mathf.Pow(basePos.x, 2) + Mathf.Pow(basePos.z, 2)) - 10386)); }
        gradient = new Vector3(gradient.x, -1.75f * Mathf.Pow(-pos.y + heightBoundary, -0.65f), gradient.z);
        return gradient;
    }

    public static Vector3 GetNormalNonTerrain(Vector3 pos, bool normalized = true)
    {
        if (normalized)
            return Procedural.Noise.noiseMethods[1][2](pos, noiseScale, 0).derivative.normalized;
        else
            return Procedural.Noise.noiseMethods[1][2](pos, noiseScale, 0).derivative;
    }


    /// <summary>
    /// Checks if a point is on the edge of a surface using IVT and gradients
    /// </summary>
    /// <param name="point">Hex point to check</param>
    /// <returns>Boolean</returns>
    public bool GradientCheck(HexWorldCoord point)
    {
        Vector3 pos = HexToPos(point);
        if (pos.y <= heightBoundary + 1 && pos.y > heightBoundary - 1)
            return BoundaryGradientCheck(point);
        Vector3 gradient = GetNormal(point);
        gradient = gradient.normalized;
        if (!Land(point + PosToHex(gradient)) && Land(point - PosToHex(gradient)))
            return true;

        //Vector3 gradientHigh = GetNormal(point + PosToHex(gradient * 0.5f));
        //Vector3 gradientLow = GetNormal(point - PosToHex(gradient * 0.5f));
        //gradientHigh = gradientHigh.normalized * Chunk.sqrt3 * 0.25f;
        //gradientLow = gradientLow.normalized * Chunk.sqrt3 * 0.25f;
        //if (!Land(point + PosToHex(gradient * 0.5f) + PosToHex(gradientHigh)) && Land(point - PosToHex(gradient * 0.5f) + PosToHex(gradientLow)))
        //    return true;

        return false;
    }

    bool BoundaryGradientCheck(HexWorldCoord point)
    {
        Vector3 pos = HexToPos(point);
        Vector3 gradientAbove = GetNormalAbove(point).normalized;
        Vector3 gradientBelow = GetNormalBelow(point).normalized;
        Vector3 gradientDown = -GetNormal(point);
        gradientDown = new Vector3(gradientDown.x, 0, gradientDown.z).normalized;
        if ((!Land(point + PosToHex(gradientAbove)) || !Land(point + PosToHex(gradientBelow))) && Land(point + PosToHex(gradientDown)))
            return true;
        return false;
    }
    #endregion

    #region Conversions
    public static Vector3 HexToPos(HexWorldCoord point)
    {
        Vector3 output;
        output.x = h2P[0].x * point.x + h2P[0].y * point.y + h2P[0].z * point.z;
        output.y = h2P[1].x * point.x + h2P[1].y * point.y + h2P[1].z * point.z;
        output.z = h2P[2].x * point.x + h2P[2].y * point.y + h2P[2].z * point.z;
        return output;
    }

    public static Vector3 HexToPos(HexCoord point)
    {
        Vector3 output;
        output.x = h2P[0].x * point.x + h2P[0].y * point.y + h2P[0].z * point.z;
        output.y = h2P[1].x * point.x + h2P[1].y * point.y + h2P[1].z * point.z;
        output.z = h2P[2].x * point.x + h2P[2].y * point.y + h2P[2].z * point.z;
        return output;
    }

    public static HexWorldCoord PosToHex(Vector3 point)
    {
        HexWorldCoord output;
        output.x = p2H[0].x * point.x + p2H[0].y * point.y + p2H[0].z * point.z;
        output.y = p2H[1].x * point.x + p2H[1].y * point.y + p2H[1].z * point.z;
        output.z = p2H[2].x * point.x + p2H[2].y * point.y + p2H[2].z * point.z;
        return output;
    }

    public static ChunkCoord PosToChunk(Vector3 point)
    {
        HexWorldCoord hex = PosToHex(point);
        ChunkCoord output;
        output.x = Mathf.FloorToInt((hex.x + .5f) / ConstructiveNet.chunkSize);
        output.y = Mathf.FloorToInt((hex.y + .5f) / ConstructiveNet.chunkSize);
        output.z = Mathf.FloorToInt((hex.z + .5f) / ConstructiveNet.chunkSize);
        return output;
    }

    public static Vector3 ChunkToPos(ChunkCoord chunkCoord)
    {
        HexWorldCoord output;
        output.x = chunkCoord.x * ConstructiveNet.chunkSize;
        output.y = chunkCoord.y * ConstructiveNet.chunkSize;
        output.z = chunkCoord.z * ConstructiveNet.chunkSize;
        //output += new HexWorldCoord(1,1,1);
        return HexToPos(output);
    }

    public static int boolArrayToInt(bool[] boo)
    {
        int total = 0;
        for (int i = 0; i < boo.Length; i++)
        {
            total += (boo[i]?1:0) * (int)Mathf.Pow(2, i);
        }
        return total;
    }
    #endregion
}
