using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Corner : IEquatable<Corner>
{
    public HexCell point;
    public Ridge refRidge;

    public Corner(HexCell point, Ridge refRidge)
    {
        this.point = point;
        this.refRidge = refRidge;
    }

    #region Equals
    public bool Equals(Corner obj)
    {
        return point == obj.point && refRidge == obj.refRidge;
    }

    public override bool Equals(object obj)
    {
        return point == ((Corner)obj).point && refRidge == ((Corner)obj).refRidge;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + point.GetHashCode();
            hash = hash * 227 + refRidge.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Corner left, Corner right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(Corner left, Corner right)
    {
        return !right.Equals(left);
    }
    #endregion
}
