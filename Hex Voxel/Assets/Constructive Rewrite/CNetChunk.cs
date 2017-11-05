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

    public int[,,] RidgeInfos = new int[8, 8, 8];

    public HexWorldCoord HexOffset { get { return World.PosToHex(World.ChunkToPos(chunkCoords)); } }
    public Vector3 PosOffset { get { return World.ChunkToPos(chunkCoords); } }
    MeshFilter filter;
    MeshCollider coll;
    Mesh mesh;

    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector3> normals = new List<Vector3>();

    bool shift;
    bool ctrl;

#endregion

    #region Start & Update
    void Awake()
    {
        filter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.Clear();
    }

    void Update()
    {
        if ((Input.GetKeyUp(KeyCode.Return) || net.autoGrow) && facesToBuild.Count != 0)
        {
            for (int i = 0; i < (shift ? 10 : 1); i++)
                ConstructFromNextEdge();
        }

        shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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
        Edge nextEdge = GetNextEdge();
        List<HexCell> neighborPoints = nextEdge.FindNeighborPoints();
        if (ctrl)
        {
            print("For Edge " + nextEdge.Start + " - " + nextEdge.End);
            foreach (HexCell point in neighborPoints)
                print(point + ": " + GetNoise(point.ToHexCoord()));
        }

        neighborPoints = neighborPoints.OrderBy(neighbor => Mathf.Abs(GetNoise(neighbor.ToHexCoord()))).ToList();

        int neighborIndex = 0;
        while (!LegalFace(new Edge(nextEdge.ridge, neighborPoints[neighborIndex], this), nextEdge.vertex))
            neighborIndex++; 

        //HexCell nextVert = neighborPoints.Aggregate(
        //    (prev, next) => Mathf.Abs(GetNoise(next.ToHexCoord())) < Mathf.Abs(GetNoise(prev.ToHexCoord())) ? next : prev);

        BuildTriangle(new Edge(nextEdge.End, nextEdge.Start, neighborPoints[neighborIndex], this));
        //print(deadRidges.Count);
    }

    public void BuildFirstTriangle(HexCell start, HexCell end, HexCell origin)
    {
        Edge edge = new Edge(start, end, origin, this);
        EdgeToBuild(edge);
        BuildTriangle(edge, true);
    }

    public void BuildTriangle(HexCell start, HexCell end, HexCell origin, bool freeFloating = false)
    {
        Vector3[] posVertices = new Vector3[3]
            {World.HexToPos(start.ToHexCoord()), World.HexToPos(end.ToHexCoord()), World.HexToPos(origin.ToHexCoord())};

        for (int i = 0; i < 3; i++)
        {
            verts.Add(posVertices[i]);
            tris.Add(tris.Count);
            normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
        }

        if(facesToBuild.Count != 0 && net.showNextEdge)
            DrawRidge(facesToBuild.Peek().ridge);

        EdgeToBuild(new Edge(end, origin, start, this));
        EdgeToBuild(new Edge(origin, start, end, this));

        if(!freeFloating)
            deadRidges.Add(new Ridge(start, end, this));

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }

    public void BuildTriangle(Edge edge, bool freeFloating = false)
    {
        Vector3[] posVertices = new Vector3[3]
            {World.HexToPos(edge.Start.ToHexCoord()), World.HexToPos(edge.End.ToHexCoord()), World.HexToPos(edge.vertex.ToHexCoord())};

        for (int i = 0; i < 3; i++)
        {
            verts.Add(posVertices[i]);
            tris.Add(tris.Count);
            normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
        }

        if (facesToBuild.Count != 0 && net.showNextEdge)
            DrawRidge(facesToBuild.Peek().ridge);

        EdgeToBuild(new Edge(edge.End, edge.vertex, edge.Start, this));
        EdgeToBuild(new Edge(edge.vertex, edge.Start, edge.End, this));

        if(!freeFloating)
        deadRidges.Add(edge.ridge);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
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

    Edge GetNextEdge()
    {
        Edge edge;
        do
        {
            edge = facesToBuild.Dequeue();
        } while (!liveRidges.Contains(edge.ridge));
        liveRidges.Remove(edge.ridge);
        return edge;
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
            return false;
        if (IsoplanarFaces(face, oldVert))
            return false;
        return true;
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
        return deadRidges.Contains(new Ridge(face.Start, face.vertex, this)) || deadRidges.Contains(new Ridge(face.vertex, face.Start, this))
            || deadRidges.Contains(new Ridge(face.End, face.vertex, this)) || deadRidges.Contains(new Ridge(face.vertex, face.End, this));
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

    public bool LiveNeighborCheck(Ridge ridge)
    {
        return liveRidges.Contains(ridge) || liveRidges.Contains(new Ridge(ridge.end, ridge.start, ridge.chunk));
    }
#endregion

    #region Noise Values
    float GetNoise(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNoise(HexToWorldHex(coord));// + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNoise(HexToWorldHex(coord)); }
    }

    Vector3 GetNormal(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNormal(HexToWorldHex(coord));// + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNormal(HexToWorldHex(coord)); }
    }
    #endregion

    #region Debug
    void DrawRidge(Ridge ridge)
    {
        CNetDebug.DrawRidge(HexToPos(ridge.start), HexToPos(ridge.end), Color.red);
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
