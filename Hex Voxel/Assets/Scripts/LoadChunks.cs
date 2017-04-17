using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LoadChunks : MonoBehaviour
{
    World world;

    List<ChunkCoord> updateList = new List<ChunkCoord>();
    HashSet<ChunkCoord> updateSet = new HashSet<ChunkCoord>();
    List<ChunkCoord> buildList = new List<ChunkCoord>();

    bool reloadRenderLists;

    static ChunkCoord[] chunkPositions;
    static RenderDistance[] renderDistances = { new RenderDistance(RenderDistanceName.Short, "Assets/Resources/ShortRenderDistance.txt", 4096),
        new RenderDistance(RenderDistanceName.Medium, "Assets/Resources/MediumRenderDistance.txt", 8192),
        new RenderDistance(RenderDistanceName.Long, "Assets/Resources/LongRenderDistance.txt", 16384) };

    int timer = 0;
    ChunkCoord previousPos;

    // Use this for initialization
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        reloadRenderLists = world.reloadRenderLists;

        if (reloadRenderLists) { RewriteChunkList(); }
        
        List<ChunkCoord> closeChunkList = new List<ChunkCoord>();
        using (StreamReader sr = File.OpenText(renderDistances[(int)world.renderDistance].filename))
        {
            string line;
            ChunkCoord closeChunkCoord;
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
        ChunkCoord playerPos = World.PosToChunk(transform.position);

        if(buildList.Count == 0)
        {
            for (int i = 0; i < chunkPositions.Length; i++)
            {
                ChunkCoord newChunkPos = new ChunkCoord(chunkPositions[i].x + playerPos.x,
                    chunkPositions[i].y + playerPos.y,
                    chunkPositions[i].z + playerPos.z);

                Chunk newChunk = world.GetChunk(World.ChunkToPos(newChunkPos));

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
            Chunk chunk = world.GetChunk(World.ChunkToPos(updateList[0]));
            chunk.GeometricUpdateChunk();
            if (chunk != null)
                chunk.update = true;
            updateSet.Remove(updateList[0]);
            updateList.RemoveAt(0);
        }
    }

    public void AddToUpdateList(ChunkCoord chunk)
    {
        if (updateSet.Add(chunk))
            updateList.Add(chunk);
    }

    /// <summary>
    /// Removes Chunks that are too far away from the player
    /// </summary>
    void DeleteChunks()
    {
        if (timer == 10)
        {
            List<ChunkCoord> chunksToDelete = new List<ChunkCoord>();
            foreach (var chunk in world.chunks)
            {
                float distance = Vector3.SqrMagnitude(transform.position - World.ChunkToPos(chunk.Value.chunkCoords));
                if (distance > renderDistances[(int)world.renderDistance].distance * 1.5f)
                    chunksToDelete.Add(chunk.Key);
            }
            foreach (ChunkCoord chunk in chunksToDelete)
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
            List<ChunkCoord> chunkList = new List<ChunkCoord>();
            using (TextWriter tw = new StreamWriter(renderDistanceFilenames[renderDistances]))
            {
                Debug.Log(string.Empty);
                for (int i = -40; i < 40; i++)
                {
                    for (int j = -40; j < 40; j++)
                    {
                        for (int k = -40; k < 40; k++)
                        {
                            if (Vector3.SqrMagnitude(World.ChunkToPos(World.PosToChunk(transform.position)) - World.ChunkToPos(new ChunkCoord(i, j, k))) < 4096)
                                chunkList.Add(new ChunkCoord(i, j, k));
                        }
                    }
                }
                chunkList = chunkList.OrderBy(x => Vector3.Distance(Vector3.zero, World.ChunkToPos(x))).ToList();
                foreach (var chunk in chunkList)
                {
                    tw.WriteLine(Mathf.RoundToInt(chunk.x) + " " + Mathf.RoundToInt(chunk.y) + " " + Mathf.RoundToInt(chunk.z));
                }
            }
        }
    }
}