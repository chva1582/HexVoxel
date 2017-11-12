using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct End : IEquatable<End>
{
    public HexCell point;
    public HexCell otherEnd;

    public End(HexCell point, HexCell otherEnd)
    {
        this.point = point;
        this.otherEnd = otherEnd;
    }


    #region Equals
    public bool Equals(End obj)
    {
        return point == obj.point && otherEnd == obj.otherEnd;
    }

    public override bool Equals(object obj)
    {
        return point == ((End)obj).point && otherEnd == ((End)obj).otherEnd;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + point.GetHashCode();
            hash = hash * 227 + otherEnd.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(End left, End right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(End left, End right)
    {
        return !right.Equals(left);
    }
    #endregion
}
