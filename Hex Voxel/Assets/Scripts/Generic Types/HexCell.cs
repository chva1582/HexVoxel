//Cell Identifier in each Chunk in Hexagonal Coordinates
using System;
using UnityEngine;

[Serializable]
public struct HexCell
{
    sbyte x, y, z;

    public sbyte X
    {
        get
        {
            return x;
        }

        set
        {
            x = value;
            //if (!(value >= 0 && value <= 8))
            //{
            //    Debug.LogError("The dimension x is out of bounds for this HexCell");
            //    x = value;
            //}
            //else
            //    x = value;
        }
    }

    public sbyte Y
    {
        get
        {
            return y;
        }

        set
        {
            y = value;
            //if (!(value >= 0 && value <= 8))
            //{
            //    Debug.LogError("The dimension y is out of bounds for this HexCell");
            //    y = value;
            //}
            //else
            //    y = value;
        }
    }

    public sbyte Z
    {
        get
        {
            return z;
        }

        set
        {
            z = value;
            //if (!(value >= 0 && value <= 8))
            //{
            //    Debug.LogError("The dimension z is out of bounds for this HexCell");
            //    z = value;
            //}
            //else
            //    z = value;
        }
    }

    public HexCell(sbyte x, sbyte y, sbyte z) : this()
    {
        X = x;
        Y = y;
        Z = z;
    }

    public HexCell(int x, int y, int z) : this()
    {
        X = (sbyte)x;
        Y = (sbyte)y;
        Z = (sbyte)z;
    }

    public override bool Equals(object obj)
    {
        if (GetHashCode() == obj.GetHashCode())
            return true;
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + X.GetHashCode();
            hash = hash * 227 + Y.GetHashCode();
            hash = hash * 227 + Z.GetHashCode();
            return hash;
        }
    }

    public static HexCell operator +(HexCell w1, HexCell w2)
    {
        return new HexCell((sbyte)(w1.X + w2.X), (sbyte)(w1.Y + w2.Y), (sbyte)(w1.Z + w2.Z));
    }

    public static HexCell operator -(HexCell w1, HexCell w2)
    {
        return new HexCell((sbyte)(w1.X - w2.X), (sbyte)(w1.Y - w2.Y), (sbyte)(w1.Z - w2.Z));
    }

    public static bool operator ==(HexCell w1, HexCell w2)
    {
        return w1.Equals(w2);
    }

    public static bool operator !=(HexCell w1, HexCell w2)
    {
        return !w1.Equals(w2);
    }

    public HexCoord ToHexCoord()
    {
        HexCoord output = new HexCoord();
        output.x = X;
        output.y = Y;
        output.z = Z;
        return output;
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }
}

