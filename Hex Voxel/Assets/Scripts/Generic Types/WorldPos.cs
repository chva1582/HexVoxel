using UnityEngine;
using System.Collections;
using System;

[Serializable]
public struct WorldPos
{
    public int x, y, z;

    public WorldPos(int x, int y, int z)
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

    public static WorldPos operator +(WorldPos w1, WorldPos w2)
    {
        return new WorldPos(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public Vector3 ToVector3()
    {
        Vector3 output = new Vector3();
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