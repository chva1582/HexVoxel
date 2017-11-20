using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Peak : IEquatable<Peak>
{
    public HexCell point;

    public Peak(HexCell p)
    {
        point = p;
    }

    #region Equals
    public bool Equals(Peak obj)
    {
        return point == obj.point;
    }

    public override bool Equals(object obj)
    {
        return point == ((Peak)obj).point;
    }

    public override int GetHashCode()
    {
        return point.GetHashCode();
    }

    public static bool operator ==(Peak left, Peak right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(Peak left, Peak right)
    {
        return !right.Equals(left);
    }
    #endregion
}
