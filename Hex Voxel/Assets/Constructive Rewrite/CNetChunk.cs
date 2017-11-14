//A group of points of square size organized in octahedral coordinates
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class CNetChunk : MonoBehaviour
{
    #region Variables
    public ConstructiveNet net;

    public ChunkCoord chunkCoords;

    public Queue<Edge> facesToBuild = new Queue<Edge>();
    HashSet<Ridge> deadRidges = new HashSet<Ridge>();
    HashSet<Ridge> liveRidges = new HashSet<Ridge>();
    Edge nextEdge;
    bool needToFindNextEdge;

    public int[,,] RidgeInfos = new int[8, 8, 8];

    public HexWorldCoord HexOffset { get { return World.PosToHex(World.ChunkToPos(chunkCoords)); } }
    public Vector3 PosOffset { get { return World.ChunkToPos(chunkCoords); } }
    MeshFilter filter;
    MeshCollider coll;
    Mesh mesh;

    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector3> normals = new List<Vector3>();

    public Edge NextEdge
    {
        get
        {
            if(needToFindNextEdge)
            {
                do
                {
                    nextEdge = facesToBuild.Dequeue();
                } while (!liveRidges.Contains(nextEdge.ridge));
                liveRidges.Remove(nextEdge.ridge);
                needToFindNextEdge = false;
            }
            return nextEdge;
        }
    }

    public List<HexCell> NextNeighbors { get { return NextEdge.FindNeighborPoints(); } }
    public Vector3 NextStartPos{ get{ return HexToPos(NextEdge.ridge.start); } }
    public Vector3 NextEndPos{ get{ return HexToPos(NextEdge.ridge.end); } }
    public Vector3 NextOldVertex{ get{ return HexToPos(NextEdge.vertex); } }

    HexCell forcedNextNeighbor = new HexCell();
    bool shouldForceNextNeighbor = false;
    public HexCell ForcedNextNeighbor
    {
        get
        {
            shouldForceNextNeighbor = false;
            return forcedNextNeighbor;
        }
        set
        {
            shouldForceNextNeighbor = true;
            forcedNextNeighbor = value;
            ConstructFromForcedNeighbor();
        }
    }

    bool leftShift;
    bool rightShift;
    bool ctrl;
    #endregion

    #region Start & Update
    void Awake()
    {
        filter = GetComponent<MeshFilter>();
        coll = GetComponent<MeshCollider>();
        mesh = new Mesh();
        mesh.Clear();
    }

    void Update()
    {
        if ((Input.GetKeyUp(KeyCode.Return) || net.autoGrow) && facesToBuild.Count != 0)
        {
            for (int i = 0; i < (rightShift ? 100 : (leftShift ? 10 : 1)); i++)
                ConstructFromNextEdge();
        }

        leftShift = Input.GetKey(KeyCode.LeftShift);
        rightShift = Input.GetKey(KeyCode.RightShift);
        ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    public void Restart()
    {
        mesh.Clear();
        verts.Clear();
        tris.Clear();
        normals.Clear();
        deadRidges.Clear();
        liveRidges.Clear();
        facesToBuild.Clear();
    }
    #endregion

    #region Build
    public void ConstructFromNextEdge()
    {
        List<HexCell> neighborPoints = NextEdge.FindNeighborPoints();
        neighborPoints = OrganizeNeighbors(neighborPoints, NextEdge.ridge);

        if (ctrl)
        {
            print("For Edge " + NextEdge.Start + " - " + NextEdge.End);
            foreach (HexCell point in neighborPoints)
                print(point + ": " + GetNoise(point.ToHexCoord()));
        }

        int neighborIndex = 0;
        while (!LegalFace(new Edge(NextEdge.ridge, neighborPoints[neighborIndex]), NextEdge.vertex))
            neighborIndex++; 

        //HexCell nextVert = neighborPoints.Aggregate(
        //    (prev, next) => Mathf.Abs(GetNoise(next.ToHexCoord())) < Mathf.Abs(GetNoise(prev.ToHexCoord())) ? next : prev);

        BuildTriangle(new Edge(NextEdge.End, NextEdge.Start, neighborPoints[neighborIndex]));
    }

    void ConstructFromForcedNeighbor()
    {
        BuildTriangle(new Edge(NextEdge.End, nextEdge.Start, ForcedNextNeighbor));
    }

    public void BuildFirstTriangle(HexCell start, HexCell end, HexCell origin)
    {
        Edge edge = new Edge(start, end, origin);
        EdgeToBuild(edge);
        BuildTriangle(edge, true);
    }

    public void BuildTriangle(HexCell start, HexCell end, HexCell origin, bool freeFloating = false)
    {
        needToFindNextEdge = true;

        Vector3[] posVertices = new Vector3[3]
            {World.HexToPos(start.ToHexCoord()), World.HexToPos(end.ToHexCoord()), World.HexToPos(origin.ToHexCoord())};

        HexCell[] hexVertices = new HexCell[3] { start, end, origin };

        for (int i = 0; i < 3; i++)
        {
            if (net.smoothMesh) { verts.Add(posVertices[i] + GetSmoothFactor(hexVertices[i])); }
            else { verts.Add(posVertices[i]); }
            tris.Add(tris.Count);
            //normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
        }

        //if(facesToBuild.Count != 0 && net.showNextEdge)
        //    DrawRidge(facesToBuild.Peek().ridge);

        EdgeToBuild(new Edge(end, origin, start));
        EdgeToBuild(new Edge(origin, start, end));

        if(!freeFloating)
            deadRidges.Add(new Ridge(start, end));

        print(mesh.vertexCount + ", " + mesh.triangles.Count());
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        coll.sharedMesh = mesh;
    }

    public void BuildTriangle(Edge edge, bool freeFloating = false)
    {
        needToFindNextEdge = true;

        Vector3[] posVertices = new Vector3[3]
            {World.HexToPos(edge.Start.ToHexCoord()), World.HexToPos(edge.End.ToHexCoord()), World.HexToPos(edge.vertex.ToHexCoord())};

        HexCell[] hexVertices = new HexCell[3] { edge.Start, edge.End, edge.vertex };

        for (int i = 0; i < 3; i++)
        {
            if(net.smoothMesh) { verts.Add(posVertices[i] + GetSmoothFactor(hexVertices[i])); }
            else { verts.Add(posVertices[i]); }
            tris.Add(tris.Count);
            //normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
        }

        //if (facesToBuild.Count != 0 && net.showNextEdge)
        //    DrawRidge(facesToBuild.Peek().ridge);

        EdgeToBuild(new Edge(edge.End, edge.vertex, edge.Start));
        EdgeToBuild(new Edge(edge.vertex, edge.Start, edge.End));

        if(!freeFloating)
        deadRidges.Add(edge.ridge);
        
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        coll.sharedMesh = mesh;
    }
    #endregion

    void EdgeToBuild(Edge edge)
    {
        if (!LiveNeighborCheck(edge.ridge))
        {
            facesToBuild.Enqueue(edge);
            liveRidges.Add(edge.ridge);
            //print("Added Live Edge: Start " + edge.ridge.start + ", End " + edge.ridge.end);
            //print(edge.ridge.Type.ToString() + ", " + edge.ridge.Direction + " :" + edge.ridge.start);
            //print(edge.ridge.YChange);
        }
        else
        {
            liveRidges.Remove(edge.ridge);
            liveRidges.Remove(edge.ridge.ReversedRidge);
            deadRidges.Add(edge.ridge);
            //print("Added Dead Edge: Start " + edge.ridge.start + ", End " + edge.ridge.end);
        }
    }

    List<HexCell> OrganizeNeighbors(List<HexCell> neighborPoints, Ridge ridge, bool fudgeFactor = false)
    {
        neighborPoints = neighborPoints.OrderBy(neighbor => Mathf.Abs(GetNoise(neighbor.ToHexCoord()))).ToList();
        if (fudgeFactor)
        {
            float minimum = Mathf.Abs(GetNoise(neighborPoints[0].ToHexCoord()));
            List<HexCell> similarWithLiveEdge = new List<HexCell>();
            bool triggered = false;
            for (int i = 1; i < neighborPoints.Count; i++)
            {
                if (Mathf.Abs(GetNoise(neighborPoints[i].ToHexCoord())) - minimum < 0.1)
                {
                    if (LiveNeighborCheck(new Edge(ridge, neighborPoints[i])))
                    {
                        similarWithLiveEdge.Add(neighborPoints[i]);
                        neighborPoints.Remove(neighborPoints[i]);
                    }
                    triggered = true;
                }
                else
                    break;
            }
            if (triggered)
            {
                if (LiveNeighborCheck(new Edge(ridge, neighborPoints[0])))
                {
                    List<HexCell> minimumPoint = new List<HexCell>();
                    minimumPoint.Add(neighborPoints[0]);
                    neighborPoints.Remove(neighborPoints[0]);
                    neighborPoints = minimumPoint.Concat(similarWithLiveEdge).Concat(neighborPoints).ToList();
                }
                else
                {
                    neighborPoints = similarWithLiveEdge.Concat(neighborPoints).ToList();
                }
            }
        }
        return neighborPoints;
    }

    Vector3 GetSmoothFactor(HexCell hex)
    {
        Vector3 norm = GetNormal(hex).normalized * Mathf.Sqrt(3) / 2;
        float A = GetNoise(hex.ToHexCoord() + World.PosToHex(norm).ToHexCoord());
        float B = GetNoise(hex.ToHexCoord() - World.PosToHex(norm).ToHexCoord());
        return -norm * (A + B) / (A - B);
    }

    #region Checks
    /// <summary>
    /// Checks if the planned face violates any geometric rules
    /// </summary>
    /// <param name="face">Face to check</param>
    /// <returns>If face is legal</returns>
    public bool LegalFace(Edge face, HexCell oldVert)
    {
        if (DeadNeighbor(face))
        {
            if (ctrl) { print("Dead Neighbor"); }
            return false;
        }

        if (IsoplanarFaces(face, oldVert))
        {
            if (ctrl) { print("Isoplanar"); }
            return false;
        }

        if (FoldBack(face, oldVert))
        {
            if (AntiNormal(face))
            {
                if (ctrl) { print("Fold Back"); }
                return false;
            }
            if (ctrl) { print("Concave Fold"); }
        }
        return true;
    }

    public Color LegalStatus(Edge face, HexCell oldVert)
    {
        Color color = new Color(0, 0, 0);
        if (DeadNeighbor(face))
            color += new Color(0, 1, 0);
        if (IsoplanarFaces(face, oldVert))
            color += new Color(0, 0, 1);
        if(FoldBack(face, oldVert))
        {
            if (AntiNormal(face))
                color += new Color(1, 0, 0);
        }
        return color;
    }

    /// <summary>
    /// Checks if a planned face will contact a pre-existing dead ridge
    /// Only non-ridge edges will be checked
    /// </summary>
    /// <param name="face">Face to check</param>
    /// <returns>If non-ridge segments are dead</returns>
    bool DeadNeighbor(Edge face)
    {
        if (deadRidges.Count == 0)
            return false;
        return deadRidges.Contains(new Ridge(face.Start, face.vertex))
            || deadRidges.Contains(new Ridge(face.End, face.vertex));
    }

    /// <summary>
    /// Checks if the new face lies in the same plane as the old one
    /// Only returns true if the surface "folds" making a zero volume space
    /// </summary>
    /// <param name="face">Face to Check</param>
    /// <param name="oldVert">Vertex of Previous Face</param>
    /// <returns>If the new face intersects the previous face</returns>
    bool IsoplanarFaces(Edge face, HexCell oldVert)
    {
        HexCell ridgeVector = face.Start - face.End;
        HexCell verticesVector = face.vertex - oldVert;
        return ((ridgeVector == verticesVector) || (ridgeVector == -1 * verticesVector));
    }

    /// <summary>
    /// Checks if a new face folds back on itself trapping it to run parallel to the actual face
    /// </summary>
    /// <param name="face">Face to Check</param>
    /// <param name="oldVert">Vertex of Previous Face</param>
    /// <returns>If the new face is a fold back</returns>
    bool FoldBack(Edge face, HexCell oldVert)
    {
        bool newPositive = 0 < Vector3.Dot(face.SeperationPlaneNormal, World.HexToPos((face.vertex - face.Start).ToHexCoord().ToHexWorldCoord()));
        bool oldPositive = 0 < Vector3.Dot(face.SeperationPlaneNormal, World.HexToPos((oldVert - face.Start).ToHexCoord().ToHexWorldCoord()));
        return newPositive == oldPositive;
    }

    /// <summary>
    /// Checks if a new face is facing in the opposite direction as the noise
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    bool AntiNormal(Edge face)
    {
        Vector3 faceNormal = Vector3.Cross(World.HexToPos((face.vertex - face.Start).ToHexCoord().ToHexWorldCoord()), World.HexToPos((face.End - face.Start).ToHexCoord().ToHexWorldCoord()));
        return Vector3.Angle(faceNormal, GetNormal(face.Start.ToHexCoord())) > 100;
    }

    /// <summary>
    /// Checks if an edge's non-ridge segments are currently live
    /// </summary>
    /// <param name="edge">Edge to check</param>
    /// <returns>If one of the edge's are live</returns>
    bool LiveNeighborCheck(Edge edge)
    {
        bool startRidge = liveRidges.Contains(new Ridge(edge.Start, edge.vertex));
        bool endRidge = liveRidges.Contains(new Ridge(edge.End, edge.vertex));
        return startRidge || endRidge;
    }

    /// <summary>
    /// Checks if ridge is live
    /// </summary>
    /// <param name="ridge">Ridge to check</param>
    /// <returns>If ridge is alive</returns>
    public bool LiveNeighborCheck(Ridge ridge)
    {
        return liveRidges.Contains(ridge);
    }
    #endregion

    #region Noise Values
    public float GetNoise(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNoise(HexToWorldHex(coord));// + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNoise(HexToWorldHex(coord)); }
    }

    public float GetNoise(HexCell cell)
    {
        HexCoord coord = cell.ToHexCoord();
        try
        {
            return net.world.GetNoise(HexToWorldHex(coord));// + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNoise(HexToWorldHex(coord)); }
    }

    public Vector3 GetNormal(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNormal(HexToWorldHex(coord));// + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNormal(HexToWorldHex(coord)); }
    }

    public Vector3 GetNormal(HexCell cell)
    {
        HexCoord coord = cell.ToHexCoord();
        try
        {
            return net.world.GetNormal(HexToWorldHex(coord));// + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNormal(HexToWorldHex(coord)); }
    }
    #endregion

    #region Conversions
    /// <summary>
    /// Converts from World Position to Hex Coordinates
    /// </summary>
    /// <param name="point">World Position</param>
    /// <returns>Hex Coordinate</returns>
    public HexCoord PosToHex(Vector3 point)
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
    public Vector3 HexToPos(HexCell point)
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

    #region Structs
    struct CNetChunkCorners
    {
        public List<Ridge> xp;
        public List<Ridge> xn;
        public List<Ridge> yp;
        public List<Ridge> yn;
        public List<Ridge> zp;
        public List<Ridge> zn;

        public CNetChunkCorners(List<Ridge> xp, List<Ridge> xn, List<Ridge> yp, List<Ridge> yn, List<Ridge> zp, List<Ridge> zn)
        {
            this.xp = xp;
            this.xn = xn;
            this.yp = yp;
            this.yn = yn;
            this.zp = zp;
            this.zn = zn;
        }
    }
    #endregion
}
