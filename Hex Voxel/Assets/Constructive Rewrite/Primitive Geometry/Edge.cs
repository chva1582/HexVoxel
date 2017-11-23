using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Edge : IEquatable<Edge>
{
    public Ridge ridge;
    public HexCell vertex;

    #region Properties
    public HexCell Start { get { return ridge.start; } }
    public HexCell End { get { return ridge.end; } }

    public Vector3 SeperationPlaneNormal
    {
        get
        {
            Vector3 ridgeDir = ridge.DirectionVector.normalized;
            return new Vector3(ridgeDir.z, 0, -1 * ridgeDir.x);
        }
    }

    public Vector3 GeometricNormal
    {
        get
        {
            Vector3 sReal = World.HexToPos(Start.ToHexCoord());
            Vector3 eReal = World.HexToPos(End.ToHexCoord());
            Vector3 vReal = World.HexToPos(vertex.ToHexCoord());
            return Vector3.Cross(vReal - sReal, eReal - sReal);
        }
    }
    #endregion

    #region PreCalculated Values
    static HexCell p0 = new HexCell(0, 0, 1), p0l = new HexCell(1, -1, 1), p0hf = new HexCell(0, 1, 1), p0hd = new HexCell(-1,2,0),
        p1 = new HexCell(1, 0, 1), p1h = new HexCell(0, 1, 0), p1lf = new HexCell(2, -1, 1), p1ld = new HexCell(2, -2, 1),
        p2 = new HexCell(1, 0, 0), p2l = new HexCell(1, -1, 0), p2hf = new HexCell(0, 1, -1), p2hd = new HexCell(-1, 2, -1),
        p3 = new HexCell(0, 0, -1), p3h = new HexCell(-1, 1, -1), p3lf = new HexCell(0, -1, -1), p3ld = new HexCell(1, -2, 0),
        p4 = new HexCell(-1, 0, -1), p4l = new HexCell(0, -1, 0), p4hf = new HexCell(-2, 1, -1), p4hd = new HexCell(-2, 2, -1),
        p5 = new HexCell(-1, 0, 0), p5h = new HexCell(-1, 1, 0), p5lf = new HexCell(0, -1, 1), p5ld = new HexCell(1, -2, 1);

    public static List<HexCell>[] flatRidgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p5,p0l,p5h,p0hf,p5lf,p1h,p4l},
        new List<HexCell>(){p2,p0,p1h,p0l,p1lf,p0hf,p2l,p5h},
        new List<HexCell>(){p3,p1,p2l,p1h,p2hf,p1lf,p3h,p0l},
        new List<HexCell>(){p4,p2,p3h,p2l,p3lf,p2hf,p4l,p1h},
        new List<HexCell>(){p5,p3,p4l,p3h,p4hf,p3lf,p5h,p2l},
        new List<HexCell>(){p0,p4,p5h,p4l,p5lf,p4hf,p0l,p3h}
    };

    public static List<HexCell>[] longRidgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p0,p1h,p5h,},//p0l,p0hd},
        new List<HexCell>(){p2,p1,p2l,p0l,},//p1h,p1ld},
        new List<HexCell>(){p3,p2,p3h,p1h,},//p2l,p2hd},
        new List<HexCell>(){p4,p3,p4l,p2l,},//p3h,p3ld},
        new List<HexCell>(){p5,p4,p5h,p3h,},//p4l,p4hd},
        new List<HexCell>(){p0,p5,p0l,p4l,},//p5h,p5ld}
    };

    public static List<HexCell>[] rightRidgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p1,p0,p2,p5,p2l,p4l,p1lf,p5lf,},//p1h,p5h,p0hf,p3ld},
        new List<HexCell>(){p2,p1,p3,p0,p3h,p5h,p2hf,p0hf,},//p2l,p0l,p1lf,p4hd},
        new List<HexCell>(){p3,p2,p4,p1,p4l,p0l,p3lf,p1lf,},//p3h,p1h,p2hf,p5ld},
        new List<HexCell>(){p4,p3,p5,p2,p5h,p1h,p4hf,p2hf,},//p4l,p2l,p3lf,p0hd},
        new List<HexCell>(){p5,p4,p0,p3,p0l,p2l,p5lf,p3lf,},//p5h,p3h,p4hf,p1ld},
        new List<HexCell>(){p0,p5,p1,p4,p1h,p3h,p0hf,p4hf,},//p0l,p4l,p5lf,p2hd}
    };

    public static List<HexCell>[] maceRidgeNeighbors = new List<HexCell>[6]
    {
        new List<HexCell>(){p5h,p1h,p0hf,p3h},
        new List<HexCell>(){p0l,p2l,p1lf,p4l},
        new List<HexCell>(){p1h,p3h,p2hf,p5h},
        new List<HexCell>(){p2l,p4l,p3lf,p0l},
        new List<HexCell>(){p3h,p5h,p4hf,p1h},
        new List<HexCell>(){p4l,p0l,p5lf,p2l}
    };

    public static List<Ridge>[] opposingRidges = new List<Ridge>[18]
    {
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p5h, p1), new Ridge(p1h, p0)},
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p0l, p2), new Ridge(p2l, p1)},
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p1h, p3), new Ridge(p3h, p2)},
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p2l, p4), new Ridge(p4l, p3)},
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p3h, p5), new Ridge(p5h, p4)},
        new List<Ridge>(){},
        new List<Ridge>(){},
        new List<Ridge>(){new Ridge(p4l, p0), new Ridge(p0l, p5)}
    };
    #endregion

    #region Constructors
    public Edge(Ridge ridge, HexCell vertex)
    {
        this.ridge = ridge;
        this.vertex = vertex;
    }

    public Edge(HexCell start, HexCell end, HexCell vertex)
    {
        ridge = new Ridge(start, end);
        this.vertex = vertex;
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
        Ridge ridge = this.ridge;
        HexCell startPoint = Start;
        HexCell endPoint = End;
        HexCell vertex = this.vertex;

        if (ridge.Type == RidgeType.Flat)
            neighbors = flatRidgeNeighbors[ridge.Direction];
        else if (ridge.Type == RidgeType.Long)
            neighbors = longRidgeNeighbors[ridge.Direction];
        else
            neighbors = rightRidgeNeighbors[ridge.Direction];

        neighbors = neighbors.Select(x => x + startPoint).ToList();
        //neighbors = (from neighbor in neighbors
        //            where (!chunk.DeadNeighborCheck(new Edge(ridge,neighbor,chunk)) &&
        //                !((startPoint - endPoint == neighbor - vertex) || (startPoint - endPoint == vertex - neighbor)))
        //            select neighbor).ToList();
        neighbors.Remove(vertex);
        return neighbors;
    }
    #endregion

    #region Equals
    public bool Equals(Edge obj)
    {
        bool forward = (ridge == obj.ridge && vertex == obj.vertex);
        return forward;
    }

    public override bool Equals(object obj)
    {
        bool forward = (ridge == ((Edge)obj).ridge && vertex == ((Edge)obj).vertex);
        return forward;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + ridge.GetHashCode();
            hash = hash * 227 + vertex.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Edge left, Edge right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(Edge left, Edge right)
    {
        return !right.Equals(left);
    }
    #endregion
}
