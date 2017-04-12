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
    public bool pointLoc;
    public bool showNormals;
    public bool areaLoad;
    public bool offsetLand;
    public bool smoothLand;
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
        string vertStringFull = PlayerPrefs.GetString("Vertices Dictionary");
        string triStringFull = PlayerPrefs.GetString("Triangles Dictionary");

        string[] arrayItemsV = vertStringFull.Split(new char[] { '|' });
        string[] arrayItemsT = triStringFull.Split(new char[] { '|' });
        foreach (string item in arrayItemsV)
        {
            string vertsString = item;
            List<HexCell> vertList = new List<HexCell>();
            foreach (string vert in vertsString.Split(new char[] { '.' }))
            {
                if (vert == "")
                    continue;
                HexCell extractedVert = new HexCell();
                extractedVert.x = sbyte.Parse(vert.Split(new char[] { ',' })[0]);
                extractedVert.y = sbyte.Parse(vert.Split(new char[] { ',' })[1]);
                extractedVert.z = sbyte.Parse(vert.Split(new char[] { ',' })[2]);
                vertList.Add(extractedVert);
            }
            vertLookup.Add(vertList.ToArray());
        }
        vertLookup.RemoveAt(64);
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
        triLookup.RemoveAt(64);
    }
    #endregion

    #region Start, Update
    // Use this for initialization
    void Start()
    {
        if (!areaLoad)
        {
            CreateChunk(new ChunkCoord(3, -3, 3));
            CreateChunk(new ChunkCoord(3, -2, 3));
        }
        LookupTableConstruction();
    }

    void Update()
    {
        
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.O))
            debugMode = debugMode != DebugMode.Octahedron ? DebugMode.Octahedron : DebugMode.None;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.G))
            debugMode = debugMode != DebugMode.Gradient ? DebugMode.Gradient : DebugMode.None;
        if(Input.GetKeyDown(KeyCode.Return))
            CreateChunk(new ChunkCoord(2, 0, -1));
    }
    #endregion

    #region Chunk Control
    /// <summary>
    /// Instantiates a new Chunk and sets it up
    /// </summary>
    /// <param name="pos">Chunk Coords of the Chunk to be created</param>
    public void CreateChunk(ChunkCoord pos)
    {
        if (chunks.ContainsKey(pos))//This is kind of cheating this should have already been checked by some were getting through
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
            chunkScript.chunkCoords = pos;
            chunkScript.world = GetComponent<World>();
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
    /// Eliminates a Chunk at given Chunk Coordiantes
    /// </summary>
    /// <param name="chunkCoord">Chunk Coordinates</param>
    public void DestroyChunk(ChunkCoord chunkCoord)
    {
        Chunk targetChunk = GetChunk(ChunkToPos(chunkCoord));
        if (targetChunk == null)
            print(targetChunk.chunkCoords);
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
            Vector3 gradient = Procedural.Noise.noiseMethods[1][2](hex.ToHexWorldCoord().ToVector3(), noiseScale).derivative * 20 + new Vector3(0, thresDropOff, 0);
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

    public static bool Land(HexWorldCoord point)
    {
        return GetNoise(point) < threshold;
    }

    public static float GetNoise(HexWorldCoord pos)
    {
        float noiseVal = Procedural.Noise.noiseMethods[1][2](pos.ToVector3(), noiseScale).value * 20 + pos.y * thresDropOff;
        return noiseVal;
    }

    public static Vector3 GetNormal(HexWorldCoord pos)
    {
            return Procedural.Noise.noiseMethods[1][2](pos.ToVector3(), noiseScale).derivative * 20 + new Vector3(0, thresDropOff, 0);
    }

    public static Vector3 GetNormalNonTerrain(Vector3 pos, bool normalized = true)
    {
        if (normalized)
            return Procedural.Noise.noiseMethods[1][2](pos, noiseScale).derivative.normalized;
        else
            return Procedural.Noise.noiseMethods[1][2](pos, noiseScale).derivative;
    }
    #endregion

    #region Conversions
    public Vector3 HexToPos(HexWorldCoord point)
    {
        Vector3 output;
        output.x = h2P[0].x * point.x + h2P[0].y * point.y + h2P[0].z * point.z;
        output.y = h2P[1].x * point.x + h2P[1].y * point.y + h2P[1].z * point.z;
        output.z = h2P[2].x * point.x + h2P[2].y * point.y + h2P[2].z * point.z;
        return output;
    }

    public Vector3 HexToPos(HexCoord point)
    {
        Vector3 output;
        output.x = h2P[0].x * point.x + h2P[0].y * point.y + h2P[0].z * point.z;
        output.y = h2P[1].x * point.x + h2P[1].y * point.y + h2P[1].z * point.z;
        output.z = h2P[2].x * point.x + h2P[2].y * point.y + h2P[2].z * point.z;
        return output;
    }

    public HexWorldCoord PosToHex(Vector3 point)
    {
        HexWorldCoord output;
        output.x = p2H[0].x * point.x + p2H[0].y * point.y + p2H[0].z * point.z;
        output.y = p2H[1].x * point.x + p2H[1].y * point.y + p2H[1].z * point.z;
        output.z = p2H[2].x * point.x + p2H[2].y * point.y + p2H[2].z * point.z;
        return output;
    }

    public ChunkCoord PosToChunk(Vector3 point)
    {
        HexWorldCoord hex = PosToHex(point);
        ChunkCoord output;
        output.x = Mathf.FloorToInt((hex.x + .5f) / Chunk.chunkSize);
        output.y = Mathf.FloorToInt((hex.y + .5f) / Chunk.chunkSize);
        output.z = Mathf.FloorToInt((hex.z + .5f) / Chunk.chunkSize);
        return output;
    }

    public Vector3 ChunkToPos(ChunkCoord chunkCoord)
    {
        HexWorldCoord output;
        output.x = chunkCoord.x * Chunk.chunkSize;
        output.y = chunkCoord.y * Chunk.chunkSize;
        output.z = chunkCoord.z * Chunk.chunkSize;
        output += new HexWorldCoord(1,1,1);
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
