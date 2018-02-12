//A group of points of square size organized in octahedral coordinates
//Also includes the mesh that connects those points
//Applied to the CNetChunk Prefab
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

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
    public Stack<Edge> undoneEdgesToBuild = new Stack<Edge>();
    HashSet<Ridge> deadRidges = new HashSet<Ridge>();
    public HashSet<Ridge> liveRidges = new HashSet<Ridge>();
    Edge nextEdge;
    bool needToFindNextEdge;

    //Properties
    public HexWorldCoord HexOffset { get { return World.PosToHex(ChunkToPos(chunkCoords)); } }
    public Vector3 PosOffset { get { return ChunkToPos(chunkCoords); } }
    int FaceCount { get { return verts.Count / 3; } }
    
    //Next Properties
    public Edge NextEdge
    {
        get
        {
            if (needToFindNextEdge)
            {
                do
                {
                    Edge edge;
                    if (undoneEdgesToBuild.Count > 0)
                        edge = undoneEdgesToBuild.Pop();
                    else
                        edge = edgesToBuild.Dequeue();
                    nextEdge = edge;
                    edge = new Edge();
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

    //Temp Debug Variables
    bool recursionOccured;

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
        if (net.autoGrow && edgesToBuild.Count != 0)
        {
            for (int i = 0; i < (rightShift ? 10 : 1); i++)
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
            Profiler.BeginSample("Build");
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
            Profiler.EndSample();
        }
        catch(ArgumentOutOfRangeException e)
        {
            Profiler.BeginSample("Deconstruct");
            //print("Recursion occured in mesh");
            recursionOccured = true;
            UnbuildTriangle(NextEdge);
            Profiler.EndSample();
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

        if (WithinChunk(origin) || !net.constrainToChunk)
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

        if (WithinChunk(edge.vertex) || !net.constrainToChunk)
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

    /// <summary>
    /// Removes a triangle from the chunk's mesh
    /// </summary>
    /// <param name="edge">Edge of the facet to be removed</param>
    public void UnbuildTriangle(Edge edge, bool isUndo = false)
    {
        HexCell otherVert = new HexCell();
        int faceIndex = -1;
        if (FindOtherVert(edge.ridge, ref otherVert, ref faceIndex))
        {
            Ridge startRidge = new Ridge(edge.Start, otherVert);
            Ridge endRidge = new Ridge(edge.End, otherVert);

            //print("OG: " + edge.Start + " - " + edge.End);
            //foreach (var liveRidge in liveRidges)
            //{
            //    print(liveRidge.start + " - " + liveRidge.end);
            //}

            liveRidges.Remove(edge.ridge);
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
                QueueUpEdge(new Edge(startRidge, edge.End), isUndo);
            }
            if (DeadNeighbor(endRidge))
            {
                deadRidges.Remove(endRidge);
                liveRidges.Add(endRidge);
                QueueUpEdge(new Edge(endRidge, edge.Start), isUndo);
            }

            verts.RemoveAt(3 * faceIndex + 2);
            verts.RemoveAt(3 * faceIndex + 1);
            verts.RemoveAt(3 * faceIndex);

            MeshCalculate(true);
        }
        if(!isUndo)
            needToFindNextEdge = true;
    }

    /// <summary>
    /// Removes a triangle from the chunk's mesh
    /// </summary>
    /// <param name="ridge">Ridge to remove a specific triangle from</param>
    public void UnbuildTriangle(Ridge ridge, bool isUndo = false)
    {
        HexCell otherVert = new HexCell();
        int faceIndex = -1;
        if(FindOtherVert(ridge, ref otherVert, ref faceIndex))
        {
            Ridge startRidge = new Ridge(ridge.start, otherVert);
            Ridge endRidge = new Ridge(ridge.end, otherVert);

            //print("OG: " + ridge.start + " - " + ridge.end);
            foreach (var liveRidge in liveRidges)
            {
                print(liveRidge.start + " - " + liveRidge.end);
            }

            liveRidges.Remove(ridge);
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
                QueueUpEdge(new Edge(startRidge, ridge.end), isUndo);
            }
            if (DeadNeighbor(endRidge))
            {
                deadRidges.Remove(endRidge);
                liveRidges.Add(endRidge);
                QueueUpEdge(new Edge(endRidge, ridge.start), isUndo);
            }

            verts.RemoveAt(3 * faceIndex + 2);
            verts.RemoveAt(3 * faceIndex + 1);
            verts.RemoveAt(3 * faceIndex);

            MeshCalculate(true);
        }
        if(!isUndo)
            needToFindNextEdge = true;
    }

    /// <summary>
    /// Removes a triangle from the chunk's mesh
    /// </summary>
    /// <param name="index">Saved index of the triangle</param>
    public void UnbuildTriangle(int index, bool isUndo = false)
    {
        Vector3 v1 = verts[3 * index];
        Vector3 v2 = verts[3 * index + 1];
        Vector3 v3 = verts[3 * index + 2];

        HexCell p1 = World.PosToHex(v1).ToHexCoord().ToHexCell();
        HexCell p2 = World.PosToHex(v2).ToHexCoord().ToHexCell();
        HexCell p3 = World.PosToHex(v3).ToHexCoord().ToHexCell();

        Ridge r1 = new Ridge(p1, p2);
        Ridge r2 = new Ridge(p2, p3);
        Ridge r3 = new Ridge(p3, p1);

        if (LiveNeighbor(r1))
        {
            liveRidges.Remove(r1);
        }
        if (LiveNeighbor(r2))
        {
            liveRidges.Remove(r2);
        }
        if (LiveNeighbor(r3))
        {
            liveRidges.Remove(r3);
        }

        if (DeadNeighbor(r1))
        {
            deadRidges.Remove(r1);
            liveRidges.Add(r1);
            QueueUpEdge(r1.ReversedRidge, isUndo);
        }
        if (DeadNeighbor(r2))
        {
            deadRidges.Remove(r2);
            liveRidges.Add(r2);
            QueueUpEdge(r2.ReversedRidge, isUndo);
        }
        if (DeadNeighbor(r3))
        {
            deadRidges.Remove(r3);
            liveRidges.Add(r3);
            QueueUpEdge(r3.ReversedRidge, isUndo);
        }

        for (int i = 0; i < 3; i++)
            verts.RemoveAt(3 * index);

        MeshCalculate(true);

        if(!isUndo)
            needToFindNextEdge = true;
    }

    /// <summary>
    /// Add edge to a quick calculate stack
    /// </summary>
    /// <param name="edge">Edge to be added</param>
    /// <param name="isUndo">Is this edge an undo</param>
    void QueueUpEdge(Edge edge, bool isUndo = false)
    {
        if (isUndo)
            undoneEdgesToBuild.Push(edge);
        else
            edgesToBuild.Enqueue(edge);
    }

    /// <summary>
    /// Add edge to a quick calculate stack
    /// </summary>
    /// <param name="ridge">Ridge of the edge to be added</param>
    /// <param name="isUndo">Is this edge an undo</param>
    void QueueUpEdge(Ridge ridge, bool isUndo = false)
    {
        HexCell vertex = new HexCell();
        if (FindOtherVert(ridge, ref vertex))
        {
            Edge edge = new Edge(ridge, vertex);
            if (isUndo)
                undoneEdgesToBuild.Push(edge);
            else
                edgesToBuild.Enqueue(edge);
        }
    }

    /// <summary>
    /// Finds the third vertex on a face in the mesh
    /// </summary>
    /// <param name="ridge">Ridge of the two already known vertices</param>
    /// <param name="cell">Output of the found vertex</param>
    /// <param name="faceIndex">Index of the face in the mesh</param>
    /// <returns>Has the face been found</returns>
    bool FindOtherVert(Ridge ridge, ref HexCell cell, ref int faceIndex)
    {
        List<HexCell> neighborPoints = ridge.FindNeighborPoints();
        
        for (int i = 0; i < FaceCount; i++)
        {
            List<HexCell> cornersOfFace = GetVerticesFromIndex(i);
            for (int j = 0; j < 3; j++)
            {
                if (cornersOfFace[j] == ridge.start)
                {
                    //if (recursionOccured)
                    //    print("Yeah");
                    cornersOfFace.Remove(cornersOfFace[j]);
                    for (int k = 0; k < 2; k++)
                    {
                        if (cornersOfFace[k] == ridge.end)
                        {
                            cornersOfFace.Remove(cornersOfFace[k]);
                            cell = cornersOfFace[0];
                            faceIndex = i;
                            return true;
                        }
                    }
                    cornersOfFace = GetVerticesFromIndex(i);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Finds the third vertex on a face in the mesh
    /// </summary>
    /// <param name="ridge">Ridge of the two already known vertices</param>
    /// <param name="cell">Output of the found vertex</param>
    /// <returns>Has the face been found</returns>
    bool FindOtherVert(Ridge ridge, ref HexCell cell)
    {
        List<HexCell> neighborPoints = ridge.FindNeighborPoints();

        for (int i = 0; i < FaceCount; i++)
        {
            List<HexCell> cornersOfFace = GetVerticesFromIndex(i);
            if (cornersOfFace.Count != 3)
                print("Without Index: " + cornersOfFace.Count);
            for (int j = 0; j < 3; j++)
            {
                if (cornersOfFace[j] == ridge.start)
                {
                    cornersOfFace.Remove(cornersOfFace[j]);
                    for (int k = 0; k < 2; k++)
                    {
                        if (cornersOfFace[k] == ridge.end)
                        {
                            cornersOfFace.Remove(cornersOfFace[k]);
                            cell = cornersOfFace[0];
                            return true;
                        }
                    }
                    cornersOfFace = GetVerticesFromIndex(i);
                }
            }
        }
        return false;
    }

    List<HexCell> GetVerticesFromIndex(int index)
    {
        List<HexCell> output = new List<HexCell>();
        for (int i = 0; i < 3; i++)
            output.Add(World.PosToHex(verts[3 * index + i]).ToHexCoord().ToHexCell());
        return output;
    }

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
            QueueUpEdge(edge);
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
