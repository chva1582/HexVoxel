using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct FaceEdge
{
    public Edge edge;
    public HexCell vertex;
    public CNetChunk chunk;

    public HexCell Start { get { return edge.start; } }
    public HexCell End { get { return edge.end; } }

    #region PreCalculated Values
    static HexCell p0 = new HexCell(0, 0, 1), p0l = new HexCell(1, -1, 1), p0hf = new HexCell(1, 1, 0),
        p1 = new HexCell(1, 0, 1), p1h = new HexCell(0, 1, 0), p1lf = new HexCell(2, -1, 1),
        p2 = new HexCell(1, 0, 0), p2l = new HexCell(1, -1, 0), p2hf = new HexCell(0, 1, -1),
        p3 = new HexCell(0, 0, -1), p3h = new HexCell(-1, 1, -1), p3lf = new HexCell(-1, -1, 0),
        p4 = new HexCell(-1, 0, -1), p4l = new HexCell(0, -1, 0), p4hf = new HexCell(-2, 1, -1),
        p5 = new HexCell(-1, 0, 0), p5h = new HexCell(-1, 1, 0), p5lf = new HexCell(0, -1, 1);

    static List<HexCell>[] flatEdgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p5,p0l,p5h,p0hf,p5lf,p1h,p4l},
        new List<HexCell>(){p2,p0,p1h,p0l,p1lf,p0hf,p2l,p5h},
        new List<HexCell>(){p3,p1,p2l,p1h,p2hf,p1lf,p3h,p0l},
        new List<HexCell>(){p4,p2,p3h,p2l,p3lf,p2hf,p4l,p1h},
        new List<HexCell>(){p5,p3,p4l,p3h,p4hf,p3lf,p5h,p2l},
        new List<HexCell>(){p0,p4,p5h,p4l,p5lf,p4hf,p0l,p3h}
    };

    static List<HexCell>[] longEdgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p0,p1h,p5h},
        new List<HexCell>(){p2,p1,p2l,p0l},
        new List<HexCell>(){p3,p2,p3h,p1h},
        new List<HexCell>(){p4,p3,p4l,p2l},
        new List<HexCell>(){p5,p4,p5h,p3h},
        new List<HexCell>(){p0,p5,p0l,p4l}
    };

    static List<HexCell>[] rightEdgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p0,p2,p5,p2l,p4l,p1lf,p5lf},
        new List<HexCell>(){p2,p1,p3,p0,p3h,p5h,p2hf,p0hf},
        new List<HexCell>(){p3,p2,p4,p1,p4l,p0l,p3lf,p1lf},
        new List<HexCell>(){p4,p3,p5,p2,p5h,p1h,p4hf,p2hf},
        new List<HexCell>(){p5,p4,p0,p3,p0l,p2l,p5lf,p3lf},
        new List<HexCell>(){p0,p5,p1,p4,p1h,p3h,p0hf,p4hf}
    };
    #endregion

    #region Constructors
    public FaceEdge(Edge edge, HexCell vertex, CNetChunk chunk)
    {
        this.edge = edge;
        this.vertex = vertex;
        this.chunk = chunk;
    }

    public FaceEdge(HexCell start, HexCell end, HexCell vertex, CNetChunk chunk)
    {
        edge = new Edge(start, end, chunk);
        this.vertex = vertex;
        this.chunk = chunk;
    }
    #endregion

    #region Functions
    /// <summary>
    /// Retrieve a list of possible neighbor vertices for this edge
    /// </summary>
    /// <returns>List of Neighbors</returns>
    public List<HexCell> FindNeighborPoints()
    {
        List<HexCell> neighbors = new List<HexCell>();
        Edge edge = this.edge;
        HexCell startPoint = edge.start;
        CNetChunk chunk = this.chunk;

        if (edge.Type == EdgeType.Flat)
            neighbors = flatEdgeNeighbors[edge.Direction];
        else if (edge.Type == EdgeType.Long)
            neighbors = longEdgeNeighbors[edge.Direction];
        else
            neighbors = rightEdgeNeighbors[edge.Direction];

        neighbors = neighbors.Select(x => x + startPoint).ToList();
        neighbors = (from neighbor in neighbors
                    where !chunk.DeadNeighborCheck(new FaceEdge(edge,neighbor,chunk))
                    select neighbor).ToList();
        neighbors.Remove(vertex);
        return neighbors;
    }
    #endregion
}
