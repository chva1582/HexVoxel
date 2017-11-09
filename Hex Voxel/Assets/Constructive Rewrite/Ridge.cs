//Edge type for representing the edge of a face
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum RidgeType { Flat, Long, Right, Mace };

public struct Ridge
{
    #region Variables & Properties
    //End Point should be clockwise from the Start Point relative to 
    //the center of the face the edge is a part of.
    public HexCell start, end;

    public CNetChunk chunk;

    //Type of Edge either Flat, Right, or Long
    public RidgeType Type
    {
        get
        {
            //if (YChange > 1f)
            //    return RidgeType.Mace;
            if (Distance > 4.1f)
                return RidgeType.Long;
            if (YChange > 0.1f)
                return RidgeType.Right;
            else
                return RidgeType.Flat;
        }
    }

    //Direction Value is 0-5 where 0 is straight forward and 3 is straight backward (like a half-clock on the XZ plane)
    public int Direction
    {
        get
        {
            return (int)(Angle / 60 + 0.001f);
        }
    }

    float Distance
    {
        get
        {
            return Vector3.SqrMagnitude(DirectionVector);
        }
    }

    public Vector3 DirectionVector
    {
        get
        {
            return World.HexToPos((start - end).ToHexCoord().ToHexWorldCoord());
        }
    }

    float Angle
    {
        get
        {
            float angle = Vector3.SignedAngle(World.HexToPos(end.ToHexCoord().ToHexWorldCoord() -
                start.ToHexCoord().ToHexWorldCoord()), Vector3.forward, -1 * Vector3.up);
            return angle < 0 ? angle + 360 : angle;
        }
    }

    public float YChange
    {
        get
        {
            return Mathf.Abs((World.HexToPos(start.ToHexCoord().ToHexWorldCoord()) - World.HexToPos(end.ToHexCoord().ToHexWorldCoord())).y);
        }
    }

    public Ridge ReversedRidge
    {
        get
        {
            return new Ridge(end, start, chunk);
        }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Constructor for Ridge
    /// </summary>
    /// <param name="startPoint">CC side point</param>
    /// <param name="endPoint">CW side point</param>
    /// <param name="cNetChunk">Parent Chunk</param>
    public Ridge(HexCell startPoint, HexCell endPoint, CNetChunk cNetChunk)
    {
        start = startPoint;
        end = endPoint;
        chunk = cNetChunk;
        
        
    }
    #endregion

    #region Overrides
    public override bool Equals(object obj)
    {
        return (start == ((Ridge)obj).start && end == ((Ridge)obj).end) || (start == ((Ridge)obj).end && end == ((Ridge)obj).start);
        //if (GetHashCode() == obj.GetHashCode())
        //    return true;
        //return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + start.GetHashCode();
            hash = hash * 227 + end.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Ridge left, Ridge right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Ridge left, Ridge right)
    {
        return !left.Equals(right);
    }
    #endregion

    #region Obsolete
    /*
    /// <summary>
    /// Constructor for Edge
    /// </summary>
    /// <param name="startPoint">CC side point</param>
    /// <param name="endPoint">CW side point</param>
    /// <param name="center">Center of the face</param>
    /// <param name="edgeType">Type of Edge to be created</param>
    public Edge(HexCell startPoint, HexCell endPoint, HexCell originPoint, EdgeType edgeType)
    {
        start = startPoint;
        end = endPoint;
        origin = originPoint;
        this.edgeType = edgeType;
    }

    ///// <summary>
    ///// Derive Edge Type from points
    ///// </summary>
    ///// <param name="startPoint">CC side point</param>
    ///// <param name="endPoint">CW side point</param>
    ///// <returns>Type of Edge created</returns>
    //EdgeType GetEdgeType(HexWorldCell startPoint, HexWorldCell endPoint)
    //{
    //    float distance = Vector3.SqrMagnitude((startPoint - endPoint).ToHexWorldCoord().ToVector3());
    //    if (distance > 1.1f)
    //        return EdgeType.Long;
    //    if ((startPoint - endPoint).y > 0.1f)
    //        return EdgeType.Right;
    //    return EdgeType.Flat;
    //}

    ///// <summary>
    ///// Rotates a point 60 degrees clockwise around its face's center
    ///// </summary>
    ///// <param name="i">Vector to be rotated</param>
    ///// <returns>Rotated Vector</returns>
    //public HexCell RotateCW60(HexCell i)
    //{
    //    HexCell o = new HexCell();
    //    o.x = i.z;
    //    o.y = i.y;
    //    o.z = (sbyte)(i.z - i.x);
    //    return o;
    //}

    ///// <summary>
    ///// Rotates a point 60 degrees counterclockwise around its face's center
    ///// </summary>
    ///// <param name="i">Vector to be rotated</param>
    ///// <returns>Rotated Vector</returns>
    //public HexCell RotateCC60(HexCell i)
    //{
    //    HexCell o = new HexCell();
    //    o.x = (sbyte)(i.x - i.z);
    //    o.y = i.y;
    //    o.z = i.x;
    //    return o;
    //}

    ///// <summary>
    ///// Rotates a point 120 degrees clockwise around its face's center
    ///// </summary>
    ///// <param name="i">Vector to be rotated</param>
    ///// <returns>Rotated Vector</returns>
    //public HexCell RotateCW120(HexCell i)
    //{
    //    HexCell o = new HexCell();
    //    o.x = (sbyte)(i.z - i.x);
    //    o.y = i.y;
    //    o.z = (sbyte)(-i.x);
    //    return o;
    //}

    ///// <summary>
    ///// Rotates a point 120 degrees counterclockwise around its face's center
    ///// </summary>
    ///// <param name="i">Vector to be rotated</param>
    ///// <returns>Rotated Vector</returns>
    //public HexCell RotateCC120(HexCell i)
    //{
    //    HexCell o = new HexCell();
    //    o.x = (sbyte)(-i.z);
    //    o.y = i.y;
    //    o.z = (sbyte)(i.x - i.z);
    //    return o;
    //}
    */
    #endregion
}
