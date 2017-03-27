using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    Chunk chunk;
    public bool isSolid;
    public WorldPos center;

    public List<Vector3> verts = new List<Vector3>();
    public List<int> tris = new List<int>();
    public List<Vector3> normals = new List<Vector3>();

    List<Vector3> vertTemp = new List<Vector3>();
    List<int> vertFail = new List<int>();
    List<int> vertSuccess = new List<int>();

    private int vertCount;
    public int VertCount{ get{ return vertCount; } set{ vertCount = value; }}

    bool[] bits;

    public Vertex(Chunk targetChunk, WorldPos position, int inVertCount)
    {
        chunk = targetChunk;
        center = position;
        VertCount = inVertCount;
    }

    public void Build()
    {
        GetHitList();
        bool[] hitList = new bool[6];
        foreach (int success in vertSuccess)
            hitList[success] = true;

        int[] tempTriArray;
        Vector3[] tempVertArray;

        tempTriArray = World.triLookup[World.boolArrayToInt(hitList)];
        tempVertArray = World.vertLookup[World.boolArrayToInt(hitList)];

        if (tempTriArray == null)
            tempTriArray = new int[0];
        if (tempVertArray == null)
            tempVertArray = new Vector3[0];

        List<int> tempTempTri = new List<int>();
        foreach (int tri in tempTriArray)
            tempTempTri.Add(tri + vertCount);

        List<Vector3> temptempVert = new List<Vector3>();
        foreach (Vector3 vert in tempVertArray)
            temptempVert.Add(vert + center.ToVector3());

        tris = tempTempTri;
        verts = temptempVert;

        BuildThirdSlant();
    }

    void GetHitList()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 vert = (center + Chunk.hexPoints[i].ToWorldPos()).ToVector3();
            if (chunk.CheckHit(vert))
            {
                vertTemp.Add(vert);
                vertSuccess.Add(i);
            }
            else
                vertFail.Add(i);
        }
    }

    #region Lookup Construction
    public void BuildLookupTables()
    {
        string totalVert = string.Empty;
        string totalTri = string.Empty;
        for (int i = 0; i < 64; i++)
        {
            VertCount = 0;
            verts.Clear();
            tris.Clear();
            vertTemp.Clear();
            vertSuccess.Clear();
            vertFail.Clear();
            center = new WorldPos(0, 0, 0);
            BitArray b = new BitArray(new int[] { i });
            bool[] bits = new bool[6];
            for (int index = 0; index < 6; index++)
            {
                bits[index] = b[index];
            }
            GetHitListForLookup(bits);
            switch (vertSuccess.Count)
            {
                case 6:
                    if (World.sixPointActive)
                        BuildOctahedron();
                    break;
                case 5:
                    if (World.fivePointActive)
                        BuildPyramid(vertFail[0]);
                    break;
                case 4:
                    if (World.fourVertSquareActive)
                    {
                        if (vertFail[0] == 4 && vertFail[1] == 5)
                        {
                            BuildPlane(vertFail[0]);
                            break;
                        }
                        else if (vertFail[0] == 2 && vertFail[1] == 3)
                        {
                            BuildPlane(vertFail[0]);
                            break;
                        }
                        else if (vertFail[0] == 0 && vertFail[1] == 1)
                        {
                            BuildPlane(vertFail[0]);
                            break;
                        }
                    }
                    if (World.fourTetraActive)
                    {
                        if (vertSuccess[2] == 4 && vertSuccess[3] == 5)
                        {
                            BuildCorner(vertSuccess[0], vertSuccess[1]);
                            break;
                        }
                        else if (vertFail[0] == 0 || vertFail[0] == 1)
                        {
                            BuildNewTetrahedron(vertFail[0], vertFail[1]);
                            break;
                        }
                        else
                        {
                            BuildTetrahedron(vertSuccess[2], vertSuccess[3]);
                            break;
                        }
                    }
                    break;
                case 3:
                    if (World.threePointActive)
                        BuildTriangle();
                    break;
                default:
                    break;
            }
            string vertString = string.Empty;
            string triString = string.Empty;
            foreach (var vert in verts)
            {
                vertString += ((int)vert.x).ToString() + "," + ((int)vert.y).ToString() + "," + ((int)vert.z).ToString() + ".";
            }
            foreach (var tri in tris)
            {
                triString += tri.ToString() + ".";
            }
            totalVert += vertString + "|";
            totalTri += triString + "|";
        }
        Debug.Log(totalTri);
        Debug.Log(totalVert);
        //
        PlayerPrefs.SetString("Vertices Dictionary", totalVert);
        PlayerPrefs.SetString("Triangles Dictionary", totalTri);
        //Debug.Log(PlayerPrefs.GetString("Vertices Dictionary"));
        //Debug.Log(PlayerPrefs.GetString("Triangles Dictionary"));
        //BuildThirdSlant();
    }

    void GetHitListForLookup(bool[] hits)
    {
        
        for (int i = 0; i < 6; i++)
        {
            Vector3 vert = (center + Chunk.hexPoints[i].ToWorldPos()).ToVector3();
            if (hits[i])
            {
                vertTemp.Add(vert);
                vertSuccess.Add(i);
            }
            else
                vertFail.Add(i);
        }
    }
    #endregion

    #region Manual
    public void BuildManual()
    {
        GetHitList();
        switch (vertSuccess.Count)
        {
            case 6:
                if (World.sixPointActive)
                    BuildOctahedron();
                break;
            case 5:
                if (World.fivePointActive)
                    BuildPyramid(vertFail[0]);
                break;
            case 4:
                if (World.fourVertSquareActive)
                {
                    if (vertFail[0] == 4 && vertFail[1] == 5)
                    {
                        BuildPlane(vertFail[0]);
                        break;
                    }
                    else if (vertFail[0] == 2 && vertFail[1] == 3)
                    {
                        BuildPlane(vertFail[0]);
                        break;
                    }
                    else if (vertFail[0] == 0 && vertFail[1] == 1)
                    {
                        BuildPlane(vertFail[0]);
                        break;
                    }
                }
                if (World.fourTetraActive)
                {
                    if (vertSuccess[2] == 4 && vertSuccess[3] == 5)
                    {
                        BuildCorner(vertSuccess[0], vertSuccess[1]);
                        break;
                    }
                    else if (vertFail[0] == 0 || vertFail[0] == 1)
                    {
                        BuildNewTetrahedron(vertFail[0], vertFail[1]);
                        break;
                    }
                    else
                    {
                        BuildTetrahedron(vertSuccess[2], vertSuccess[3]);
                        break;
                    }
                }
                break;
            case 3:
                if (World.threePointActive)
                    BuildTriangle();
                break;
            default:
                break;
        }
        BuildThirdSlant();
    }

    void BuildOctahedron()
    {
        for (int face = 0, faceIndex = 0; face < 8; face++)
        {
            int[] triTemp = { (face / 2) % 2 == 0 ? 0 : 1, face % 2 == 0 ? 4 : 5, (face / 4) % 2 == 0 ? 2 : 3 };
            if (face == 1 || face == 2 || face == 4 || face == 7)
                Array.Reverse(triTemp);
            Vector3 faceVec = Vector3.Cross(Chunk.hexPoints[triTemp[1]] - Chunk.hexPoints[triTemp[0]], Chunk.hexPoints[triTemp[2]] - Chunk.hexPoints[triTemp[0]]);
            if (chunk.TriNormCheck(center.ToVector3(), faceVec.normalized) || !World.sixFaceCancel)
            {
                int i = 0;
                foreach (int tri in triTemp)
                {
                    verts.Add(vertTemp[tri]);
                    tris.Add(VertCount + 3 * faceIndex + i);
                    normals.Add(Chunk.GetNormal(center.ToVector3()));
                    i++;
                }
                faceIndex++;
            }
        }
    }

    void BuildPyramid(int failPoint)
    {
        //Top Build
        int vertFailOpposite = failPoint % 2 == 0 ? failPoint + 1 : failPoint - 1;
        for (int face = 0, faceIndex = 0; face < 8; face++)
        {
            int[] triTemp = { face < 4 ? 0 : 1, (face / 2) % 2 == 0 ? 2 : 3, face % 2 == 0 ? 4 : 5 };
            if (triTemp[(failPoint / 2)] != failPoint)
            {
                if (face == 0 || face == 3 || face == 5 || face == 6)
                    Array.Reverse(triTemp);
                Vector3 faceVec = Vector3.Cross(Chunk.hexPoints[triTemp[1]] - Chunk.hexPoints[triTemp[0]], Chunk.hexPoints[triTemp[2]] - Chunk.hexPoints[triTemp[0]]);
                if (chunk.TriNormCheck(center.ToVector3(), faceVec.normalized) || !World.fiveFaceCancel)
                {
                    int i = 0;
                    foreach (int tri in triTemp)
                    {
                        verts.Add(vertTemp[failPoint <= tri ? tri - 1 : tri]);
                        tris.Add(VertCount + 3 * faceIndex + i);
                        normals.Add(Chunk.GetNormal(center.ToVector3()));
                        i++;
                    }
                    faceIndex++;
                }
            }
        }
        //Flat Part
        VertCount += verts.Count;
        vertTemp.Remove(failPoint % 2 == 0 ? vertTemp[vertFailOpposite - 1] : vertTemp[vertFailOpposite]);
        BuildPlane(failPoint);
    }

    void BuildPlane(int failPoint)
    {
        Vector3 faceVec = Chunk.hexPoints[failPoint];
        if (chunk.TriNormCheck(center.ToVector3(), faceVec.normalized) || !World.fourVertFaceReverse)
        {
            for (int i = 0; i < 3; i++)
            {
                verts.Add(vertTemp[i]);
                tris.Add(VertCount + 3 - i);
                normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
            for (int i = 0; i < 3; i++)
            {
                verts.Add(vertTemp[i == 2 ? 3 : i]);
                tris.Add(VertCount + 3 + i);
                normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
        }
    }

    void BuildCorner(int successPoint1, int successPoint2)
    {
        int left = successPoint2 - successPoint1 == 2 ? 1 : 0;
        int right = successPoint2 - successPoint1 == 2 ? 0 : 1;
        int[] triTemp = { left, right, 2, right, left, 3, right, left, 2, left, right, 3 };
        for (int face = 0; face < 4; face++)
        {
            for (int j = 0; j < 3; j++)
            {
                verts.Add(vertTemp[triTemp[3 * face + j]]);
                tris.Add(VertCount + 3 * face + j);
                normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
        }
    }

    void BuildNewTetrahedron(int failPoint1, int failPoint2)
    {
        int[] triTemp = { 1, 2, 0, 1, 3, 2, 1, 0, 3, 2, 3, 0 };
        for (int face = 0; face < 4; face++)
        {
            for (int j = 0; j < 3; j++)
            {
                verts.Add(vertTemp[triTemp[3 * face + j]]);
                if (failPoint1 + failPoint2 == 5)
                    tris.Add(vertCount + 3 * face + 2 - j);
                else
                    tris.Add(vertCount + 3 * face + j);
                normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
        }
    }

    void BuildTetrahedron(int corner, int point)
    {
        int[] triTemp = { 0, corner, 1, 0, 1, point, 0, point, corner, 1, corner, point };

        int i = 0;
        for (int face = 0; face < 4; face++)
        {
            Vector3 faceVec = Vector3.Cross(Chunk.hexPoints[triTemp[1 + 3 * face]] - Chunk.hexPoints[triTemp[3 * face]], Chunk.hexPoints[triTemp[2 + 3 * face]] - Chunk.hexPoints[triTemp[3 * face]]);
            if (chunk.TriNormCheck(center.ToVector3(), faceVec.normalized) || !World.fourTetraFaceCancel)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (triTemp[3 * face + j] == corner)
                        verts.Add(vertTemp[2]);
                    else if (triTemp[3 * face + j] == point)
                        verts.Add(vertTemp[3]);
                    else
                        verts.Add(vertTemp[triTemp[3 * face + j]]);
                    if (vertFail[0] + vertFail[1] == 7)
                        tris.Add(VertCount + i - j - j + 2);
                    else
                        tris.Add(VertCount + i);
                    normals.Add(Chunk.GetNormal(center.ToVector3()));
                    i++;
                }
            }
        }
    }

    void BuildTriangle()
    {
        for (int i = 0; i < 3; i++)
        {
            verts.Add(vertTemp[i]);
            normals.Add(Chunk.GetNormal(center.ToVector3()));
            Vector3 faceVec = Vector3.Cross(Chunk.hexPoints[vertSuccess[1]] - Chunk.hexPoints[vertSuccess[0]], Chunk.hexPoints[vertSuccess[2]] - Chunk.hexPoints[vertSuccess[0]]);
            if (chunk.TriNormCheck(center.ToVector3(), faceVec.normalized))
                tris.Add(VertCount + i);
            else
                tris.Add(VertCount + 2 - i);
        }
        for (int i = 0; i < 3; i++)
        {
            verts.Add(vertTemp[i]);
            normals.Add(Chunk.GetNormal(center.ToVector3()));
            Vector3 faceVec = Vector3.Cross(Chunk.hexPoints[vertSuccess[1]] - Chunk.hexPoints[vertSuccess[0]], Chunk.hexPoints[vertSuccess[2]] - Chunk.hexPoints[vertSuccess[0]]);
            if (!chunk.TriNormCheck(center.ToVector3(), faceVec.normalized))
                tris.Add(VertCount + 3 + i);
            else
                tris.Add(VertCount + 5 - i);
        }
    }

    void BuildThirdSlant()
    {
        if (chunk.CheckHit(center.ToVector3()) && chunk.CheckHit(center.ToVector3() + Chunk.hexPoints[1]) && chunk.CheckHit(center.ToVector3() - Chunk.hexPoints[3] + Chunk.hexPoints[5]) && chunk.CheckHit(center.ToVector3() + Chunk.hexPoints[2] + Chunk.hexPoints[5]) && World.thirdDiagonalActive)
        {
            vertCount += verts.Count;
            vertTemp.Clear();
            vertTemp.Add(center.ToVector3());
            vertTemp.Add(center.ToVector3() - Chunk.hexPoints[3] + Chunk.hexPoints[5]);
            vertTemp.Add(center.ToVector3() + Chunk.hexPoints[2] + Chunk.hexPoints[5]);
            vertTemp.Add(center.ToVector3());
            vertTemp.Add(center.ToVector3() + Chunk.hexPoints[2] + Chunk.hexPoints[5]);
            vertTemp.Add(center.ToVector3() + Chunk.hexPoints[1]);
            for (int i = 0; i < 6; i++)
            {
                verts.Add(vertTemp[i]);
                tris.Add(vertCount + i);
                //normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
            for (int i = 0; i < 6; i++)
            {
                verts.Add(vertTemp[5 - i]);
                tris.Add(vertCount + 6 + i);
                //normals.Add(Chunk.GetNormal(center.ToVector3()));
            }
        }
    }
    #endregion
}
