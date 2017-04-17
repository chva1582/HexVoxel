using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class Serialization
{
    static string folder = "Saves";

    public static string SaveLocation(string worldName)
    {
        string saveLocation = folder + "/" + worldName + "/";

        if (!Directory.Exists(saveLocation))
            Directory.CreateDirectory(saveLocation);

        return saveLocation;
    }

    public static string FileName(ChunkCoord coords)
    {
        string fileName = coords.x + "," + coords.y + "," + coords.z + ".bin";
        return fileName;
    }

    public static void SaveChunk(Chunk chunk)
    {
        string saveFile = SaveLocation(chunk.world.worldName);
        saveFile += FileName(chunk.chunkCoords);

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None);
        formatter.Serialize(stream, chunk.EditedData);
        stream.Close();
    }

    public static bool LoadChunk(Chunk chunk)
    {
        string saveFile = SaveLocation(chunk.world.worldName);
        saveFile += FileName(chunk.chunkCoords);

        if (!File.Exists(saveFile))
            return false;
        
        IFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(saveFile, FileMode.Open);
        NoiseData data = (NoiseData)formatter.Deserialize(stream);
        stream.Close();

        chunk.editedValues = data.values;
        chunk.editedNormals = data.normals;

        return true;
    }

}
