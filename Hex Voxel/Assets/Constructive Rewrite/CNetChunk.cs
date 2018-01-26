//A group of points of square size organized in octahedral coordinates
//Also includes the mesh that connects those points
//Applied to the CNetChunk Prefab
using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class CNetChunk : MonoBehaviour
{
    #region Variables
    //Control Variables
    int size;
    public ChunkCoord chunkCoords;

    //Parents
    public ConstructiveNet net;

    //Components
    MeshFilter filter;
    MeshCollider coll;
    Mesh mesh;

    //Mesh Variables
    public List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector3> normals = new List<Vector3>();

    //Input Variables
    bool leftShift;
    bool rightShift;
    bool ctrl;

    //Net Storage Variables
    public Queue<Edge> edgesToBuild = new Queue<Edge>();
    HashSet<Ridge> deadRidges = new HashSet<Ridge>();
    HashSet<Ridge> liveRidges = new HashSet<Ridge>();
    Edge nextEdge;
    bool needToFindNextEdge;

    //Properties
    public HexWorldCoord HexOffset { get { return World.PosToHex(ChunkToPos(chunkCoords)); } }
    public Vector3 PosOffset { get { return ChunkToPos(chunkCoords); } }

    
    public Edge NextEdge
    {
        get
        {
            if(needToFindNextEdge)
            {
                do
                {
                    nextEdge = edgesToBuild.Dequeue();
                } while (!liveRidges.Contains(nextEdge.ridge));
                liveRidges.Remove(nextEdge.ridge);
                needToFindNextEdge = false;
            }
            return nextEdge;
        }
    }
    public List<HexCell> NextNeighbors { get { return NextEdge.FindNeighborPoints(); } }
    public Vector3 NextStartPos{ get{ return HexToPos(NextEdge.ridge.start); } }
    public Vector3 NextEndPos{ get{ return HexToPos(NextEdge.ridge.end); } }
    public Vector3 NextOldVertex{ get{ return HexToPos(NextEdge.vertex); } }

    HexCell forcedNextNeighbor = new HexCell();
    bool shouldForceNextNeighbor = false;
    public HexCell ForcedNextNeighbor
    {
        get
        {
            shouldForceNextNeighbor = false;
            return forcedNextNeighbor;
        }
        set
        {
            shouldForceNextNeighbor = true;
            forcedNextNeighbor = value;
            ConstructFromForcedNeighbor();
        }
    }

    //Info
    int unbuiltTriangles = 0;
    #endregion

    #region Start & Update
    void Awake()
    {
        filter = GetComponent<MeshFilter>();
        coll = GetComponent<MeshCollider>();
        mesh = new Mesh();
        mesh.Clear();
    }

    void Update()
    {
        if ((Input.GetKeyUp(KeyCode.Return) || net.autoGrow) && edgesToBuild.Count != 0)
        {
            for (int i = 0; i < (rightShift ? 100 : (leftShift ? 10 : 1)); i++)
                ConstructFromNextEdge();
        }

        leftShift = Input.GetKey(KeyCode.LeftShift);
        rightShift = Input.GetKey(KeyCode.RightShift);
        ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    public void Restart()
    {
        mesh.Clear();
        verts.Clear();
        tris.Clear();
        normals.Clear();
        deadRidges.Clear();
        liveRidges.Clear();
        edgesToBuild.Clear();
        int destroyedFaces = 0;
    }
    #endregion

    #region Build
    public void ConstructFromNextEdge()
    {
        List<HexCell> neighborPoints = NextEdge.FindNeighborPoints();

        try
        {
            for (int i = 0; i < 8; i++)
            {
                HexCell point = neighborPoints[0];
                float minimumNoise = Mathf.Abs(GetNoise(neighborPoints[0]));
                for (int j = 1; j < neighborPoints.Count; j++)
                {
                    float nextNoise = Mathf.Abs(GetNoise(neighborPoints[j]));
                    if (nextNoise < minimumNoise)
                    {
                        minimumNoise = nextNoise;
                        point = neighborPoints[j];
                    }
                }
                if (LegalFace(new Edge(NextEdge.ridge, point), NextEdge.vertex))
                {
                    BuildTriangle(new Edge(NextEdge.End, NextEdge.Start, point));
                    break;
                }
                else
                    neighborPoints.Remove(point);
            }
        }
        catch(ArgumentOutOfRangeException e)
        {
            UnbuildTriangle(NextEdge);
        }
        finally
        {
            if(net.continueOnProblem)
                needToFindNextEdge = true;
        }
    }

    void ConstructFromForcedNeighbor()
    {
        BuildTriangle(new Edge(NextEdge.End, nextEdge.Start, ForcedNextNeighbor));
    }

    public void ConstructFirstTriangle(HexCell start, HexCell end, HexCell origin)
    {
        Edge edge = new Edge(start, end, origin);
        EdgeToBuild(edge);
        BuildTriangle(edge, true);
    }

    public void ConstructFirstTriangle(Ridge ridge)
    {
        List<HexCell> neighborPoints = ridge.FindNeighborPoints();
        
        HexCell point = neighborPoints[0];
        float minimumNoise = Mathf.Abs(GetNoise(neighborPoints[0]));
        for (int j = 1; j < neighborPoints.Count; j++)
        {
            float nextNoise = Mathf.Abs(GetNoise(neighborPoints[j]));
            if (nextNoise < minimumNoise)
            {
                minimumNoise = nextNoise;
                point = neighborPoints[j];
            }
        }
        Edge edge = new Edge(ridge, point);
        if (Vector3.Angle(-edge.GeometricNormal, GetNormal(point.ToHexCoord())) > 90)
            ridge = ridge.ReversedRidge;
        EdgeToBuild(new Edge(ridge, point));
        BuildTriangle(new Edge(ridge, point), true);
    }

    public void BuildTriangle(HexCell start, HexCell end, HexCell origin, bool freeFloating = false)
    {
        needToFindNextEdge = true;

        if (WithinChunk(origin))
        {
            Vector3[] posVertices = new Vector3[3]
                {World.HexToPos(start.ToHexCoord()), World.HexToPos(end.ToHexCoord()), World.HexToPos(origin.ToHexCoord())};

            HexCell[] hexVertices = new HexCell[3] { start, end, origin };

            for (int i = 0; i < 3; i++)
            {
                if (net.smoothMesh) { verts.Add(posVertices[i] + GetSmoothFactor(hexVertices[i])); }
                else { verts.Add(posVertices[i]); }
                //normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
            }

            EdgeToBuild(new Edge(end, origin, start));
            EdgeToBuild(new Edge(origin, start, end));

            if (!freeFloating)
                deadRidges.Add(new Ridge(start, end));

            MeshCalculate();
        }
    }

    public void BuildTriangle(Edge edge, bool freeFloating = false)
    {
        needToFindNextEdge = true;

        if (WithinChunk(edge.vertex))
        {
            Vector3[] posVertices = new Vector3[3]
                {World.HexToPos(edge.Start.ToHexCoord()), World.HexToPos(edge.End.ToHexCoord()), World.HexToPos(edge.vertex.ToHexCoord())};

            HexCell[] hexVertices = new HexCell[3] { edge.Start, edge.End, edge.vertex };

            for (int i = 0; i < 3; i++)
            {
                if (net.smoothMesh) { verts.Add(posVertices[i] + GetSmoothFactor(hexVertices[i])); }
                else { verts.Add(posVertices[i]); }
                //normals.Add(Vector3.Cross(posVertices[1] - posVertices[0], posVertices[2] - posVertices[0]));
            }

            EdgeToBuild(new Edge(edge.End, edge.vertex, edge.Start));
            EdgeToBuild(new Edge(edge.vertex, edge.Start, edge.End));

            if (!freeFloating)
                deadRidges.Add(edge.ridge);

            MeshCalculate();
        }
    }

    public void UnbuildTriangle(Edge edge)
    {
        unbuiltTriangles++;
        List<Vector3> checkVertices = new List<Vector3>() { HexToPos(edge.Start), HexToPos(edge.End), HexToPos(edge.vertex) };
        for (int i = 1; i < verts.Count; i++)
        {
            if (checkVertices.Contains(verts[verts.Count-i]))
            {
                checkVertices.Remove(verts[verts.Count - i]);
                if(checkVertices.Contains(verts[verts.Count-i-1]))
                {
                    checkVertices.Remove(verts[verts.Count - i - 1]);
                    if(checkVertices.Contains(verts[verts.Count - i - 2]))
                    {
                        Ridge startRidge = new Ridge(edge.Start, edge.vertex);
                        Ridge endRidge = new Ridge(edge.End, edge.vertex);

                        if (LiveNeighbor(startRidge))
                        {
                            liveRidges.Remove(startRidge);
                        }
                        if (LiveNeighbor(endRidge))
                        {
                            liveRidges.Remove(endRidge);
                        }

                        if (DeadNeighbor(startRidge))
                        {
                            deadRidges.Remove(startRidge);
                            liveRidges.Add(startRidge);
                            edgesToBuild.Enqueue(new Edge(startRidge, edge.End));
                        }
                        if (DeadNeighbor(endRidge))
                        {
                            deadRidges.Remove(endRidge);
                            liveRidges.Add(endRidge);
                            edgesToBuild.Enqueue(new Edge(endRidge, edge.Start));
                        }

                        verts.RemoveAt(verts.Count - i);
                        verts.RemoveAt(verts.Count - i - 1);
                        verts.RemoveAt(verts.Count - i - 2);

                        MeshCalculate(true);
                    }
                }
                checkVertices = new List<Vector3>() { HexToPos(edge.Start), HexToPos(edge.End), HexToPos(edge.vertex) };
            }
        }
    }

    //HexCell FindOldVert(Ridge ridge)
    //{
    //    List<HexCell> neighborPoints = ridge.FindNeighborPoints();
    //    foreach (var point in neighborPoints)
    //    {
    //        if (DeadNeighbor(new Ridge(ridge.start, point)) && DeadNeighbor(new Ridge(ridge.end, point)))
    //            return point;
    //    }

    //    foreach (var point in neighborPoints)
    //    {
    //        List<Vector3> checkVertices = new List<Vector3>() { HexToPos(ridge.start), HexToPos(ridge.end), HexToPos(point) };
    //        for (int i = 1; i < verts.Count; i++)
    //        {
    //            if (checkVertices.Contains(verts[verts.Count - i]))
    //            {
    //                checkVertices.Remove(verts[verts.Count - i]);
    //                if (checkVertices.Contains(verts[verts.Count - i - 1]))
    //                {
    //                    checkVertices.Remove(verts[verts.Count - i - 1]);
    //                    if (checkVertices.Contains(verts[verts.Count - i - 2]))
    //                    {

    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    void MeshCalculate(bool remove = false)
    {
        tris.Clear();
        for (int i = 0; i < verts.Count; i++)
            tris.Add(i);

        if (remove)
        {
            mesh.SetTriangles(tris, 0);
            mesh.SetVertices(verts);
        }
        else
        {
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
        }
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        coll.sharedMesh = mesh;
    }
    #endregion

    void EdgeToBuild(Edge edge)
    {
        if (!LiveNeighbor(edge.ridge))
        {
            edgesToBuild.Enqueue(edge);
            liveRidges.Add(edge.ridge);
            //print("Added Live Edge: Start " + edge.ridge.start + ", End " + edge.ridge.end);
            //print(edge.ridge.Type.ToString() + ", " + edge.ridge.Direction + " :" + edge.ridge.start);
            //print(edge.ridge.YChange);
        }
        else
        {
            liveRidges.Remove(edge.ridge);
            liveRidges.Remove(edge.ridge.ReversedRidge);
            deadRidges.Add(edge.ridge);
            //print("Added Dead Edge: Start " + edge.ridge.start + ", End " + edge.ridge.end);
        }
    }

    bool WithinChunk(HexCell hex)
    {
        bool x = hex.X >= 0 && hex.X < ConstructiveNet.chunkSize;
        bool y = hex.Y >= 0 && hex.Y < ConstructiveNet.chunkSize;
        bool z = hex.Z >= 0 && hex.Z < ConstructiveNet.chunkSize;
        return x && y && z;
    }

    #region Legality Checks
    /// <summary>
    /// Checks if the planned face violates any geometric rules
    /// </summary>
    /// <param name="face">Face to check</param>
    /// <returns>If face is legal</returns>
    public bool LegalFace(Edge face, HexCell oldVert)
    {
        if (DeadNeighbor(face))
        {
            if (ctrl) { print("Dead Neighbor"); }
            return false;
        }

        if (Collision(face))
        {
            if (ctrl) { print("Collision"); }
            return false;
        }

        if (Covered(face))
            return false;

        if (FoldBack(face, oldVert))
        {
            if (AntiNormal(face))
            {
                if (ctrl) { print("Fold Back"); }
                return false;
            }
            if (ctrl) { print("Concave Fold"); }
        }
        return true;
    }

    public Color LegalStatus(Edge face, HexCell oldVert)
    {
        Color color = new Color(0, 0, 0);
        if (DeadNeighbor(face))
            color += new Color(0, 1, 0);
        if (Collision(face))
            color += new Color(0, 0, 0.6f);
        if (Covered(face))
            color += new Color(0.4f, 0, 0.4f);
        if(FoldBack(face, oldVert))
        {
            if (AntiNormal(face))
                color += new Color(0.6f, 0, 0);
        }
        return color;
    }

    /// <summary>
    /// Checks if a planned face will overlay a pre-existing dead ridge
    /// Only non-ridge edges will be checked
    /// </summary>
    /// <param name="edge">Face to check</param>
    /// <returns>If non-ridge segments are dead</returns>
    bool DeadNeighbor(Edge edge)
    {
        if (deadRidges.Count == 0)
            return false;
        return deadRidges.Contains(new Ridge(edge.Start, edge.vertex))
            || deadRidges.Contains(new Ridge(edge.End, edge.vertex));
    }

    /// <summary>
    /// Checks if a ridge will overlay a pre-existing dead ridge
    /// </summary>
    /// <param name="edge">Face to check</param>
    /// <returns>If segment is dead</returns>
    bool DeadNeighbor(Ridge ridge)
    {
        if (deadRidges.Count == 0)
            return false;
        return deadRidges.Contains(ridge);
    }

    /// <summary>
    /// Checks if the new face lies in the same plane as the old one
    /// Only returns true if the surface "folds" making a zero volume space
    /// </summary>
    /// <param name="edge">Face to Check</param>
    /// <param name="oldVert">Vertex of Previous Face</param>
    /// <returns>If the new face intersects the previous face</returns>
    bool IsoplanarFaces(Edge edge, HexCell oldVert)
    {
        HexCell ridgeVector = edge.Start - edge.End;
        HexCell verticesVector = edge.vertex - oldVert;
        return ((ridgeVector == verticesVector) || (ridgeVector == -1 * verticesVector));
    }

    /// <summary>
    /// Checks if a new face folds back on itself trapping it to run parallel to the actual face
    /// </summary>
    /// <param name="edge">Face to Check</param>
    /// <param name="oldVert">Vertex of Previous Face</param>
    /// <returns>If the new face is a fold back</returns>
    bool FoldBack(Edge edge, HexCell oldVert)
    {
        bool newPositive = 0 < Vector3.Dot(edge.SeperationPlaneNormal, World.HexToPos((edge.vertex - edge.Start).ToHexCoord().ToHexWorldCoord()));
        bool oldPositive = 0 < Vector3.Dot(edge.SeperationPlaneNormal, World.HexToPos((oldVert - edge.Start).ToHexCoord().ToHexWorldCoord()));
        return newPositive == oldPositive;
    }

    /// <summary>
    /// Checks if a new face is facing in the opposite direction as the noise
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    bool AntiNormal(Edge edge)
    {
        return Vector3.Angle(edge.GeometricNormal, GetNormal(edge.Start.ToHexCoord())) > 100;
    }

    bool Collision(Edge edge)
    {
        //Check for Opposing Edges
        List<Ridge> opposingRidges = Edge.opposingRidges[new Ridge(edge.Start, edge.vertex).Identifier];
        for (int i = 0; i < opposingRidges.Count; i++)
        {
            if(ctrl)
            {
                print("First" + LiveNeighbor(opposingRidges[i].OffsetRidge(edge.Start)) + ", " + DeadNeighbor(opposingRidges[i].OffsetRidge(edge.Start)));
            }
            if (LiveNeighbor(opposingRidges[i].OffsetRidge(edge.Start)) || DeadNeighbor(opposingRidges[i].OffsetRidge(edge.Start)))
                return true;
        }
        opposingRidges = Edge.opposingRidges[new Ridge(edge.End, edge.vertex).Identifier];
        for (int i = 0; i < opposingRidges.Count; i++)
        {
            if (ctrl)
            {
                print("Second" + LiveNeighbor(opposingRidges[i].OffsetRidge(edge.End)) + ", " + DeadNeighbor(opposingRidges[i].OffsetRidge(edge.End)));
            }
            if (LiveNeighbor(opposingRidges[i].OffsetRidge(edge.End)) || DeadNeighbor(opposingRidges[i].OffsetRidge(edge.End)))
                return true;
        }
        return false;
    }

    bool Covered(Edge edge)
    {
        HexCoord checkPoint = edge.vertex.ToHexCoord();
        Vector3 normal = GetNormal(checkPoint).normalized;
        Ray forwardRay = new Ray(HexToPos(checkPoint) + normal * 0.5f, normal);
        Ray backwardRay = new Ray(HexToPos(checkPoint) - normal * 0.5f, -normal);
        Ray forwardReverseRay = new Ray(HexToPos(checkPoint) + normal * 2.5f, -normal);
        Ray backwardReverseRay = new Ray(HexToPos(checkPoint) - normal * 2.5f, normal);
        Debug.DrawRay(forwardReverseRay.origin, 2 * forwardReverseRay.direction, Color.red);
        Debug.DrawRay(backwardReverseRay.origin, 2 * backwardReverseRay.direction, Color.blue);
        return (Physics.Raycast(forwardRay, 2) || Physics.Raycast(backwardRay, 2) || 
            Physics.Raycast(forwardReverseRay, 2) || Physics.Raycast(backwardReverseRay, 2));
    }

    /// <summary>
    /// Checks if an edge's non-ridge segments are currently live
    /// </summary>
    /// <param name="edge">Edge to check</param>
    /// <returns>If one of the edge's are live</returns>
    bool LiveNeighbor(Edge edge)
    {
        bool startRidge = liveRidges.Contains(new Ridge(edge.Start, edge.vertex));
        bool endRidge = liveRidges.Contains(new Ridge(edge.End, edge.vertex));
        return startRidge || endRidge;
    }

    /// <summary>
    /// Checks if ridge is live
    /// </summary>
    /// <param name="ridge">Ridge to check</param>
    /// <returns>If ridge is alive</returns>
    public bool LiveNeighbor(Ridge ridge)
    {
        return liveRidges.Contains(ridge);
    }
    #endregion

    #region Noise Values
    public float GetNoise(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNoise(HexToWorldHex(coord));// + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNoise(HexToWorldHex(coord)); }
    }

    public float GetNoise(HexCell cell)
    {
        HexCoord coord = cell.ToHexCoord();
        try
        {
            return net.world.GetNoise(HexToWorldHex(coord));// + editedValues[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNoise(HexToWorldHex(coord)); }
    }

    public Vector3 GetNormal(HexCoord coord)
    {
        //HexCell cell = coord.ToHexCell();
        try
        {
            return net.world.GetNormal(HexToWorldHex(coord));// + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNormal(HexToWorldHex(coord)); }
    }

    public Vector3 GetNormal(HexCell cell)
    {
        HexCoord coord = cell.ToHexCoord();
        try
        {
            return net.world.GetNormal(HexToWorldHex(coord));// + editedNormals[cell.x, cell.y, cell.z];
        }
        catch { return net.world.GetNormal(HexToWorldHex(coord)); }
    }

    float GetAdjustedNoise(HexCell point, Ridge ridge, HexCell oldVert)
    {
        float noise = Mathf.Abs(GetNoise(point.ToHexCoord()));
        Vector3 oldNormal = (new Edge(ridge, oldVert)).GeometricNormal;
        Vector3 newNormal = (new Edge(ridge.ReversedRidge, point)).GeometricNormal;
        float angle = Mathf.Abs(Vector3.Angle(oldNormal, newNormal));
        return noise + (angle / 90f) * 0.1f;
    }

    Vector3 GetSmoothFactor(HexCell hex)
    {
        Vector3 norm = GetNormal(hex).normalized * Mathf.Sqrt(3) / 2;
        float A = GetNoise(hex.ToHexCoord() + World.PosToHex(norm).ToHexCoord());
        float B = GetNoise(hex.ToHexCoord() - World.PosToHex(norm).ToHexCoord());
        return -norm * (A + B) / (A - B);
    }
    #endregion

    #region Conversions
    /// <summary>
    /// Converts from World Position to Hex Coordinates
    /// </summary>
    /// <param name="point">World Position</param>
    /// <returns>Hex Coordinate</returns>
    public HexCoord PosToHex(Vector3 point)
    {
        point.x -= PosOffset.x;
        point.y -= PosOffset.y;
        point.z -= PosOffset.z;
        return World.PosToHex(point).ToHexCoord();
    }

    /// <summary>
    /// Converts from Hex Coordinate to World Position
    /// </summary>
    /// <param name="point">Hex Coordinate</param>
    /// <returns>World Position</returns>
    public Vector3 HexToPos(HexCell point)
    {
        Vector3 output = new Vector3();
        output = World.HexToPos(point.ToHexCoord());
        output.x += PosOffset.x;
        output.y += PosOffset.y;
        output.z += PosOffset.z;
        return output;
    }

    /// <summary>
    /// Converts from Hex Coordinate to World Position
    /// </summary>
    /// <param name="point">Hex Coordinate</param>
    /// <returns>World Position</returns>
    public Vector3 HexToPos(HexCoord point)
    {
        Vector3 output = new Vector3();
        output = World.HexToPos(point);
        output.x += PosOffset.x;
        output.y += PosOffset.y;
        output.z += PosOffset.z;
        return output;
    }

    public HexWorldCoord HexToWorldHex(HexCoord point)
    {
        return HexOffset + point;
    }

    public static ChunkCoord PosToChunk(Vector3 point)
    {
        HexWorldCoord hex = World.PosToHex(point);
        ChunkCoord output;
        output.x = Mathf.FloorToInt((hex.x + .5f) / ConstructiveNet.chunkSize);
        output.y = Mathf.FloorToInt((hex.y + .5f) / ConstructiveNet.chunkSize);
        output.z = Mathf.FloorToInt((hex.z + .5f) / ConstructiveNet.chunkSize);
        return output;
    }

    public static Vector3 ChunkToPos(ChunkCoord chunkCoord)
    {
        HexWorldCoord output;
        output.x = chunkCoord.x * ConstructiveNet.chunkSize;
        output.y = chunkCoord.y * ConstructiveNet.chunkSize;
        output.z = chunkCoord.z * ConstructiveNet.chunkSize;
        return World.HexToPos(output);
    }
    #endregion

    #region Structs
    struct CNetChunkCorners
    {
        public List<Ridge> xp;
        public List<Ridge> xn;
        public List<Ridge> yp;
        public List<Ridge> yn;
        public List<Ridge> zp;
        public List<Ridge> zn;

        public CNetChunkCorners(List<Ridge> xp, List<Ridge> xn, List<Ridge> yp, List<Ridge> yn, List<Ridge> zp, List<Ridge> zn)
        {
            this.xp = xp;
            this.xn = xn;
            this.yp = yp;
            this.yn = yn;
            this.zp = zp;
            this.zn = zn;
        }
    }
    #endregion
}
