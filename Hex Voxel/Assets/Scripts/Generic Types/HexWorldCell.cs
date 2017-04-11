//Cell Identifier in the world in Hexagonal Coordinates
using System;

[Serializable]
public struct HexWorldCell
{
    public int x, y, z;

    public HexWorldCell(int x, int y, int z)
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

    public static HexWorldCell operator +(HexWorldCell w1, HexWorldCell w2)
    {
        return new HexWorldCell(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public HexWorldCoord ToHexWorldCoord()
    {
        HexWorldCoord output = new HexWorldCoord();
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


