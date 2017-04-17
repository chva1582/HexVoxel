using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Normal
{
    float x, y, z;

    public Normal(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vector3 operator +(Vector3 w1, Normal w2)
    {
        return new Vector3(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

    public static Normal operator +(Normal w1, Vector3 w2)
    {
        return new Normal(w1.x + w2.x, w1.y + w2.y, w1.z + w2.z);
    }

}
