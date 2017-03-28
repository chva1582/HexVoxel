public enum RenderDistanceName { Short, Medium, Long }

public struct RenderDistance
{
    public RenderDistanceName name;
    public string filename;
    public int distance;

    public RenderDistance(RenderDistanceName name, string filename, int distance)
    {
        this.name = name;
        this.filename = filename;
        this.distance = distance;
    }
}
