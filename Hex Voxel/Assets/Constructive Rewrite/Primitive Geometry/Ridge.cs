//Single segment
using System;
using UnityEngine;

public enum RidgeType { Flat, Long, Right, Mace };

public struct Ridge : IEquatable<Ridge>
{
    //End Point should be clockwise from the Start Point relative to 
    //the center of the face the edge is a part of.
    public HexCell start, end;
    
    #region Properties
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
            return new Ridge(end, start);
        }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Constructor for Ridge
    /// </summary>
    /// <param name="startPoint">CC side point</param>
    /// <param name="endPoint">CW side point</param>
    public Ridge(HexCell startPoint, HexCell endPoint)
    {
        start = startPoint;
        end = endPoint;
    }
    #endregion

    #region Equals
    public bool Equals(Ridge obj)
    {
        bool forward = (start == obj.start && end == obj.end);
        bool backward = (start == obj.end && end == obj.start);
        return forward || backward;
    }

    public override bool Equals(object obj)
    {
        bool forward = (start == ((Ridge)obj).start && end == ((Ridge)obj).end);
        bool backward = (start == ((Ridge)obj).end && end == ((Ridge)obj).start);
        return forward || backward;
    }

    public override int GetHashCode()
    {
        int startHash = start.GetHashCode();
        int endHash = end.GetHashCode();
        if (startHash > endHash)
        {
            startHash = endHash;
            endHash = start.GetHashCode();
        }
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + startHash;
            hash = hash * 227 + endHash;
            return hash;
        }
    }

    public static bool operator ==(Ridge left, Ridge right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(Ridge left, Ridge right)
    {
        return !right.Equals(left);
    }
    #endregion
}
