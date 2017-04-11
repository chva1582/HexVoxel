//Cell Identifier in each Chunk in Hexagonal Coordinates
using System;

[Serializable]
public struct HexCell
{
    public sbyte x, y, z;

    public HexCell(sbyte x, sbyte y, sbyte z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public HexCell(int x, int y, int z)
    {
        this.x = (sbyte)x;
        this.y = (sbyte)y;
        this.z = (sbyte)z;
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

    public static HexCell operator +(HexCell w1, HexCell w2)
    {
        return new HexCell((sbyte)(w1.x + w2.x), (sbyte)(w1.y + w2.y), (sbyte)(w1.z + w2.z));
    }

    public static HexCell operator -(HexCell w1, HexCell w2)
    {
        return new HexCell((sbyte)(w1.x - w2.x), (sbyte)(w1.y - w2.y), (sbyte)(w1.z - w2.z));
    }

    public HexCoord ToHexCoord()
    {
        HexCoord output = new HexCoord();
        output.x = x;
        output.y = y;
        output.z = z;
        return output;
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}

