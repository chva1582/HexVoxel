using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LoadChunks : MonoBehaviour
{
    World world;

    List<WorldPos> updateList = new List<WorldPos>();
    List<WorldPos> buildList = new List<WorldPos>();

    bool reloadRenderLists;

    static WorldPos[] chunkPositions;
    static RenderDistance[] renderDistances = { new RenderDistance(RenderDistanceName.Short, "ShortRenderDistance.txt", 4096),
        new RenderDistance(RenderDistanceName.Medium, "MediumRenderDistance.txt", 8192),
        new RenderDistance(RenderDistanceName.Long, "LongRenderDistance.txt", 16384) };

    int timer = 0;
    WorldPos previousPos;

    // Use this for initialization
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        reloadRenderLists = world.reloadRenderLists;
        List<Vector3> chunkList = new List<Vector3>();

        if (reloadRenderLists) { RewriteChunkList(); }
        
        List<WorldPos> closeChunkList = new List<WorldPos>();
        using (StreamReader sr = File.OpenText(renderDistances[(int)world.renderDistance].filename))
        {
            string line;
            WorldPos closeChunkCoord;
            // Read and display lines from the file until the end of 
            // the file is reached.
            while ((line = sr.ReadLine()) != null)
            {
                string[] coordinateString = line.Split(' ');
                closeChunkCoord.x = int.Parse(coordinateString[0]);
                closeChunkCoord.y = int.Parse(coordinateString[1]);
                closeChunkCoord.z = int.Parse(coordinateString[2]);
                closeChunkList.Add(closeChunkCoord);
            }
        }
        chunkPositions = closeChunkList.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        if (world.areaLoad)
        {
            FindChunksToLoad();
            LoadAndRenderChunks();
            DeleteChunks();
        }

    }

    /// <summary>
    /// Checks nearby Chunks and decides which need to be reloaded
    /// </summary>
    void FindChunksToLoad()
    {
        WorldPos playerPos = world.PosToChunk(transform.position);

        if(buildList.Count == 0)
        {
            for (int i = 0; i < chunkPositions.Length; i++)
            {
                WorldPos newChunkPos = new WorldPos(chunkPositions[i].x + playerPos.x,
                    chunkPositions[i].y + playerPos.y,
                    chunkPositions[i].z + playerPos.z);

                Chunk newChunk = world.GetChunk(world.ChunkToPos(newChunkPos));

                if (newChunk != null
                    && (newChunk.rendered || updateList.Contains(newChunkPos)))
                    continue;

                buildList.Add(newChunkPos);
            }
        }
        if (!Equals(playerPos, previousPos))
            buildList.Clear();
        previousPos = playerPos;
    }

    /// <summary>
    /// Dictates the creation and update of chunks
    /// </summary>
    void LoadAndRenderChunks()
    {
        for (int i = 0; i < 2; i++)
        {
            if(buildList.Count != 0)
            {
                world.CreateChunk(buildList[0]);
                buildList.RemoveAt(0);
            }
        }
        for (int i = 0; i < updateList.Count; i++)
        {
            Chunk chunk = world.GetChunk(updateList[0].ToVector3());
            if (chunk != null)
                chunk.update = true;
            updateList.RemoveAt(0);
        }
    }

    /// <summary>
    /// Removes Chunks that are too far away from the player
    /// </summary>
    void DeleteChunks()
    {
        if (timer == 10)
        {
            List<WorldPos> chunksToDelete = new List<WorldPos>();
            foreach (var chunk in world.chunks)
            {
                float distance = Vector3.SqrMagnitude(transform.position - world.ChunkToPos(chunk.Value.chunkCoords));
                if (distance > renderDistances[(int)world.renderDistance].distance * 1.5f)
                    chunksToDelete.Add(chunk.Key);
            }
            foreach (WorldPos chunk in chunksToDelete)
                world.DestroyChunk(chunk);
            timer = 0;
        }
        timer++;
    }


    /// <summary>
    /// When called writes the chunks in each render distance to text file
    /// </summary>
    void RewriteChunkList()
    {
        string[] renderDistanceFilenames = { "ShortRenderDistance.txt", "MediumRenderDistance.txt", "LongRenderDistance.txt" };
        for (int renderDistances = 0; renderDistances < renderDistanceFilenames.Length; renderDistances++)
        {
            List<Vector3> chunkList = new List<Vector3>();
            using (TextWriter tw = new StreamWriter(renderDistanceFilenames[renderDistances]))
            {
                Debug.Log(string.Empty);
                for (int i = -40; i < 40; i++)
                {
                    for (int j = -40; j < 40; j++)
                    {
                        for (int k = -40; k < 40; k++)
                        {
                            if (Vector3.SqrMagnitude(world.ChunkToPos(world.PosToChunk(transform.position)) - world.ChunkToPos(new WorldPos(i, j, k))) < 4096)
                                chunkList.Add(new Vector3(i, j, k));
                        }
                    }
                }
                chunkList = chunkList.OrderBy(x => Vector3.Distance(Vector3.zero, world.ChunkToPos(x.ToWorldPos()))).ToList();
                foreach (var chunk in chunkList)
                {
                    tw.WriteLine(Mathf.RoundToInt(chunk.x) + " " + Mathf.RoundToInt(chunk.y) + " " + Mathf.RoundToInt(chunk.z));
                }
            }
        }
    }
}