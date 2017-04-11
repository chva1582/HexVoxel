//Coordinates in the world in Hexagonal Coordinates
using UnityEngine;
using System;

[Serializable]
public struct HexWorldCoord
{
    public float x, y, z;

    public HexWorldCoord(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
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
            hash = hash * 227 + x.GetHashCode();
            hash = hash * 227 + y.GetHashCode();
            hash = hash * 227 + z.GetHashCode();
            return hash;
        }
    }

    public static HexWorldCoord operator +(HexWorldCoord w1, HexWorldCoord w2)
    {
        return new HexWorldCoord(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public static HexWorldCoord operator +(HexWorldCoord w1, HexCoord w2)
    {
        return new HexWorldCoord(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public static HexWorldCoord operator -(HexWorldCoord w1, HexWorldCoord w2)
    {
        return new HexWorldCoord(w1.x - w2.x, w1.y - w2.y, w1.z - w2.z);
    }

    public static HexWorldCoord operator -(HexWorldCoord w1, HexCoord w2)
    {
        return new HexWorldCoord(w1.x - w2.x, w1.y - w2.y, w1.z - w2.z);
    }

    public Vector3 ToVector3()
    {
        Vector3 output = new Vector3();
        output.x = x;
        output.y = y;
        output.z = z;
        return output;
    }

    public HexWorldCell ToHexWorldCell()
    {
        return new HexWorldCell(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
    }

    public HexCoord ToHexCoord()
    {
        return new HexCoord(x, y, z);
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}
