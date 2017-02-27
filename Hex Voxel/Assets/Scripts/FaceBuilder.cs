using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceBuilder
{
    //Geometric Booleans
    public static bool sixPointActive = true, sixFaceCancel;
    public static bool fivePointActive = true, fiveFaceCancel;
    public static bool fourHoriSquareActive = true, fourHoriFaceReverse;
    public static bool fourVertSquareActive = true, fourVertFaceReverse;
    public static bool fourTetraActive = true, fourTetraFaceCancel;
    public static bool threePointActive = true, threeFaceReverse;
    public static bool thirdDiagonalActive = true, thirdDiagonalFaceReverse;

    public static Vector3[] hexPoints = {new Vector3(0,0,0), new Vector3(1,0,1), new Vector3(0,0,1),
        new Vector3(1,0,0),new Vector3(1,-1,1), new Vector3(0,1,0)};

    public static void Build(Vector3 center, Chunk chunk, ref List<Vector3> verts, ref List<int> tris, ref List<Vector3> normals)
    {
        float startTime = Time.realtimeSinceStartup;
        List<Vector3> vertTemp = new List<Vector3>();
        List<int> vertFail = new List<int>();
        List<int> vertSuccess = new List<int>();
        int vertCount = verts.Count;
        for (int i = 0; i < 6; i++)
        {
            Vector3 vert = center + hexPoints[i];
            if (chunk.CheckHit(vert))
            {
                vertTemp.Add(vert);
                vertSuccess.Add(i);
            }
            else
                vertFail.Add(i);
        }
        if (vertTemp.Count == 6 && sixPointActive)
        {
            //Octahedron

            for (int face = 0, faceIndex = 0; face < 8; face++)
            {
                int[] triTemp = { (face / 2) % 2 == 0 ? 0 : 1, face % 2 == 0 ? 4 : 5, (face / 4) % 2 == 0 ? 2 : 3 };
                if (face == 1 || face == 2 || face == 4 || face == 7)
                    Array.Reverse(triTemp);
                Vector3 faceVec = Vector3.Cross(hexPoints[triTemp[1]] - hexPoints[triTemp[0]], hexPoints[triTemp[2]] - hexPoints[triTemp[0]]);
                if (chunk.TriNormCheck(center, faceVec.normalized) || !sixFaceCancel)
                {
                    int i = 0;
                    foreach (int tri in triTemp)
                    {
                        verts.Add(vertTemp[tri]);
                        tris.Add(vertCount + 3 * faceIndex + i);
                        normals.Add(Chunk.GetNormal(center));
                        i++;
                    }
                    faceIndex++;
                }
            }
        }
        if (vertTemp.Count == 5 && fivePointActive)
        {
            //Rectangular Prism
            int vertFailOpposite = vertFail[0] % 2 == 0 ? vertFail[0] + 1 : vertFail[0] - 1;
            for (int face = 0, faceIndex = 0; face < 8; face++)
            {
                int[] triTemp = { face < 4 ? 0 : 1, (face / 2) % 2 == 0 ? 2 : 3, face % 2 == 0 ? 4 : 5 };
                if (triTemp[(vertFail[0] / 2)] != vertFail[0])
                {
                    if (face == 0 || face == 3 || face == 5 || face == 6)
                        Array.Reverse(triTemp);
                    Vector3 faceVec = Vector3.Cross(hexPoints[triTemp[1]] - hexPoints[triTemp[0]], hexPoints[triTemp[2]] - hexPoints[triTemp[0]]);
                    if (chunk.TriNormCheck(center, faceVec.normalized) || !fiveFaceCancel)
                    {
                        int i = 0;
                        foreach (int tri in triTemp)
                        {
                            verts.Add(vertTemp[vertFail[0] <= tri ? tri - 1 : tri]);
                            tris.Add(vertCount + 3 * faceIndex + i);
                            normals.Add(Chunk.GetNormal(center));
                            i++;
                        }
                        faceIndex++;
                    }
                }
            }
            vertCount = verts.Count;
            vertTemp.Remove(vertFail[0] % 2 == 0 ? vertTemp[vertFailOpposite - 1] : vertTemp[vertFailOpposite]);
            if (vertFail[0] == 0 || vertFail[0] == 3 || vertFail[0] == 4)
            {
                Vector3 faceVec = hexPoints[vertFail[0]];
                if (chunk.TriNormCheck(center, faceVec.normalized) || !fiveFaceCancel)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i]);
                        tris.Add(vertCount + i);
                        normals.Add(Chunk.GetNormal(center));
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i == 2 ? 3 : i]);
                        tris.Add(vertCount + 5 - i);
                        normals.Add(Chunk.GetNormal(center));
                    }
                }
            }
            else
            {
                Vector3 faceVec = hexPoints[vertFail[0]];
                if (chunk.TriNormCheck(center, faceVec.normalized) || !fiveFaceCancel)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i]);
                        tris.Add(vertCount + 2 - i);
                        normals.Add(Chunk.GetNormal(center));
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i == 2 ? 3 : i]);
                        tris.Add(vertCount + 3 + i);
                        normals.Add(Chunk.GetNormal(center));
                    }
                }
            }
        }
        if (vertSuccess.Count == 4)
        {
            if (vertFail[0] == 4 && vertFail[1] == 5 && fourHoriSquareActive)
            {
                //Horizontal Square
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[2] - hexPoints[0]);
                    if (chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + i);
                    else
                        tris.Add(vertCount + 2 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i == 2 ? 3 : i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[2] - hexPoints[0]);
                    if (!chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + 3 + i);
                    else
                        tris.Add(vertCount + 5 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[2] - hexPoints[0]);
                    if (!chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + 6 + i);
                    else
                        tris.Add(vertCount + 8 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i == 2 ? 3 : i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[2] - hexPoints[0]);
                    if (chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + 9 + i);
                    else
                        tris.Add(vertCount + 11 - i);
                }
            }
            else if (vertFail[0] == 2 && vertFail[1] == 3 && fourVertSquareActive)
            {

                //Point Slanted Square

                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[4] - hexPoints[0]);
                    if (chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + i);
                    else
                        tris.Add(vertCount + 2 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    int iTemp = i == 2 ? 3 : i;
                    verts.Add(vertTemp[iTemp]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[5] - hexPoints[0]);
                    if (chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + 3 + i);
                    else
                        tris.Add(vertCount + 5 - i);
                }
                vertCount = verts.Count;
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[4] - hexPoints[0]);
                    if (!chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + i);
                    else
                        tris.Add(vertCount + 2 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    int iTemp = i == 2 ? 3 : i;
                    verts.Add(vertTemp[iTemp]);
                    normals.Add(Chunk.GetNormal(center));
                    Vector3 faceVec = Vector3.Cross(hexPoints[1] - hexPoints[0], hexPoints[5] - hexPoints[0]);
                    if (!chunk.TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + 3 + i);
                    else
                        tris.Add(vertCount + 5 - i);
                }
            }
            else if (vertFail[0] == 0 && vertFail[1] == 1)
            {
                //Debug.Log("Vert Part 2");
            }
            else if (vertSuccess[2] == 4 && vertSuccess[3] == 5)
            {
                int left = vertSuccess[1] - vertSuccess[0] == 2 ? 1 : 0;
                int right = vertSuccess[1] - vertSuccess[0] == 2 ? 0 : 1;
                int[] triTemp = { left, right, 2, right, left, 3, right, left, 2, left, right, 3 };
                for (int face = 0; face < 4; face++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        verts.Add(vertTemp[triTemp[3 * face + j]]);
                        tris.Add(vertCount + 3 * face + j);
                        normals.Add(Chunk.GetNormal(center));
                    }
                }
            }
            else if (vertFail[0] == 0 || vertFail[0] == 1)
            {
                int[] triTemp = { 1, 2, 0, 1, 3, 2, 1, 0, 3, 2, 3, 0 };
                for (int face = 0; face < 4; face++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        verts.Add(vertTemp[triTemp[3 * face + j]]);
                        if (vertFail[0] + vertFail[1] == 5)
                            tris.Add(vertCount + 3 * face + 2 - j);
                        else
                            tris.Add(vertCount + 3 * face + j);
                        normals.Add(Chunk.GetNormal(center));
                    }
                }
            }
            else if (fourTetraActive)
            {
                //Tetrahedron
                int corner = vertSuccess[2];
                int point = vertSuccess[3];

                int[] triTemp = { 0, corner, 1, 0, 1, point, 0, point, corner, 1, corner, point };

                int i = 0;
                for (int face = 0; face < 4; face++)
                {
                    Vector3 faceVec = Vector3.Cross(hexPoints[triTemp[1 + 3 * face]] - hexPoints[triTemp[3 * face]], hexPoints[triTemp[2 + 3 * face]] - hexPoints[triTemp[3 * face]]);
                    if (chunk.TriNormCheck(center, faceVec.normalized) || !fourTetraFaceCancel)
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
                                tris.Add(vertCount + i - j - j + 2);
                            else
                                tris.Add(vertCount + i);
                            normals.Add(Chunk.GetNormal(center));
                            i++;
                        }
                    }
                }
            }

        }
        if (vertTemp.Count == 3 && threePointActive)
        {
            //Triangle
            for (int i = 0; i < 3; i++)
            {
                verts.Add(vertTemp[i]);
                normals.Add(Chunk.GetNormal(center));
                Vector3 faceVec = Vector3.Cross(hexPoints[vertSuccess[1]] - hexPoints[vertSuccess[0]], hexPoints[vertSuccess[2]] - hexPoints[vertSuccess[0]]);
                if (chunk.TriNormCheck(center, faceVec.normalized))
                    tris.Add(vertCount + i);
                else
                    tris.Add(vertCount + 2 - i);
            }
            for (int i = 0; i < 3; i++)
            {
                verts.Add(vertTemp[i]);
                normals.Add(Chunk.GetNormal(center));
                Vector3 faceVec = Vector3.Cross(hexPoints[vertSuccess[1]] - hexPoints[vertSuccess[0]], hexPoints[vertSuccess[2]] - hexPoints[vertSuccess[0]]);
                if (!chunk.TriNormCheck(center, faceVec.normalized))
                    tris.Add(vertCount + 3 + i);
                else
                    tris.Add(vertCount + 5 - i);
            }
        }
        //Third Slant Face
        if (chunk.CheckHit(center) && chunk.CheckHit(center + hexPoints[1]) && chunk.CheckHit(center - hexPoints[3] + hexPoints[5]) && chunk.CheckHit(center + hexPoints[2] + hexPoints[5]) && thirdDiagonalActive)
        {
            vertCount = verts.Count;
            vertTemp.Clear();
            vertTemp.Add(center);
            vertTemp.Add(center - hexPoints[3] + hexPoints[5]);
            vertTemp.Add(center + hexPoints[2] + hexPoints[5]);
            vertTemp.Add(center);
            vertTemp.Add(center + hexPoints[2] + hexPoints[5]);
            vertTemp.Add(center + hexPoints[1]);
            for (int i = 0; i < 6; i++)
            {
                verts.Add(vertTemp[i]);
                tris.Add(vertCount + i);
                normals.Add(Chunk.GetNormal(center));
            }
            for (int i = 0; i < 6; i++)
            {
                verts.Add(vertTemp[5 - i]);
                tris.Add(vertCount + 6 + i);
                normals.Add(Chunk.GetNormal(center));
            }
        }
    }
}
