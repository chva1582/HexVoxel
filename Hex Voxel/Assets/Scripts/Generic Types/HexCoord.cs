//Coordinates in each Chunk in Hexagonal Coordinates
using UnityEngine;
using System;

[Serializable]
public struct HexCoord
{
    public float x, y, z;

    public HexCoord(float x, float y, float z)
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

    public static HexCoord operator +(HexCoord w1, HexCoord w2)
    {
        return new HexCoord(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public static HexCoord operator -(HexCoord w1, HexCoord w2)
    {
        return new HexCoord(w1.x - w2.x, w1.y - w2.y, w1.z - w2.z);
    }

    public static HexCoord operator *(HexCoord w1, int i)
    {
        return new HexCoord(w1.x * i, w1.y * i, w1.z * i);
    }

    public Vector3 ToVector3()
    {
        Vector3 output = new Vector3();
        output.x = x;
        output.y = y;
        output.z = z;
        return output;
    }

    public HexCell ToHexCell()
    {
        return new HexCell(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
    }

    public HexWorldCoord ToHexWorldCoord()
    {
        return new HexWorldCoord(x, y, z);
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}
