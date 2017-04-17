using System;

[Serializable]
public struct NoiseData
{
    public float[,,] values;
    public Normal[,,] normals;

    public NoiseData(float[,,] values, Normal[,,] normals)
    {
        this.values = values;
        this.normals = normals;
    }
}
