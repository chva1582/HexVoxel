//Identifier Coordinate of each Chunk
using System;

[Serializable]
public struct ChunkCoord
{
    public int x, y, z;

    public ChunkCoord(int x, int y, int z)
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

    public static ChunkCoord operator +(ChunkCoord w1, ChunkCoord w2)
    {
        return new ChunkCoord(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}