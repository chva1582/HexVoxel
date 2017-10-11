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

    public Queue<FaceEdge> facesToBuild = new Queue<FaceEdge>();
    HashSet<Edge> deadEdges = new HashSet<Edge>();
    HashSet<Edge> liveEdges = new HashSet<Edge>(); 

    public int[,,] edgeInfos = new int[8, 8, 8];

    public HexWorldCoord HexOffset { get { return World.PosToHex(World.ChunkToPos(chunkCoords)); } }
    public Vector3 PosOffset { get { return World.ChunkToPos(chunkCoords); } }
    MeshFilter filter;
    MeshCollider coll;
    Mesh mesh;

    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector3> normals = new List<Vector3>();
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
        if (Input.GetKeyUp(KeyCode.Return) && facesToBuild.Count != 0)
        {
            FaceEdge nextEdge = GetNextEdge();
            List<HexCell> neighborPoints = nextEdge.FindNeighborPoints();
            //foreach (HexCell point in neighborPoints)
            //    print(point + ": " + GetNoise(point.ToHexCoord()));
            HexCell nextVert = neighborPoints.Aggregate(
                (prev, next) => Mathf.Abs(GetNoise(next.ToHexCoord())) < Mathf.Abs(GetNoise(prev.ToHexCoord())) ? next : prev);
            BuildTriangle(new FaceEdge(nextEdge.End, nextEdge.Start, nextVert, this));
        }
    }
    #endregion

    #region Build
    public void BuildFirstTriangle(HexCell start, HexCell end, HexCell origin)
    {
        FaceEdge edge = new FaceEdge(start, end, origin, this);
        EdgeToBuild(edge);
        BuildTriangle(edge);
    }

    public void BuildTriangle(HexCell start, HexCell end, HexCell origin)
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
            DrawEdge(facesToBuild.Peek().edge);

        EdgeToBuild(new FaceEdge(end, origin, start, this));
        EdgeToBuild(new FaceEdge(origin, start, end, this));

        deadEdges.Add(new Edge(start, end, this));

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }

    public void BuildTriangle(FaceEdge edge)
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
            DrawEdge(facesToBuild.Peek().edge);

        EdgeToBuild(new FaceEdge(edge.End, edge.vertex, edge.Start, this));
        EdgeToBuild(new FaceEdge(edge.vertex, edge.Start, edge.End, this));

        deadEdges.Add(edge.edge);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }
    #endregion

    void EdgeToBuild(FaceEdge edge)
    {
        if (!LiveNeighborCheck(edge.edge))
        {
            facesToBuild.Enqueue(edge);
            liveEdges.Add(edge.edge);
            print(edge.edge.Type.ToString() + ", " + edge.edge.Direction + " :" + edge.edge.start);
            print(edge.edge.YChange);
        }
        else
        {
            liveEdges.Remove(edge.edge);
            liveEdges.Remove(edge.edge.ReversedEdge);
        }
    }

    FaceEdge GetNextEdge()
    {
        FaceEdge edge;
        do
        {
            edge = facesToBuild.Dequeue();
        } while (!liveEdges.Contains(edge.edge));
        liveEdges.Remove(edge.edge);
        return edge;
    }

    public bool DeadNeighborCheck(FaceEdge face)
    {
        if (deadEdges.Count == 0)
            return false;
        return deadEdges.Contains(new Edge(face.Start, face.vertex, this)) || deadEdges.Contains(new Edge(face.vertex, face.Start, this))
            || deadEdges.Contains(new Edge(face.End, face.vertex, this)) || deadEdges.Contains(new Edge(face.vertex, face.End, this));
    }

    public bool LiveNeighborCheck(Edge edge)
    {
        return liveEdges.Contains(edge) || liveEdges.Contains(new Edge(edge.end, edge.start, edge.chunk));
    }

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
    void DrawEdge(Edge edge)
    {
        CNetDebug.DrawEdge(HexToPos(edge.start), HexToPos(edge.end), Color.red);
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
        public List<Edge> xp;
        public List<Edge> xn;
        public List<Edge> yp;
        public List<Edge> yn;
        public List<Edge> zp;
        public List<Edge> zn;

        public CNetChunkCorners(List<Edge> xp, List<Edge> xn, List<Edge> yp, List<Edge> yn, List<Edge> zp, List<Edge> zn)
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
