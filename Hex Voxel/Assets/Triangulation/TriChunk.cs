using UnityEngine;
using System.Collections.Generic;
using System;

namespace Voxel
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]

    public class TriChunk : MonoBehaviour
    {
        #region Control Variables
        public static int chunkSize = 6;
        public static int chunkHeight = 2;
        public Vector2 posOffset = new Vector2();
        public TriWorld world;
        public GameObject dot;
        static bool[,,] hits = new bool[chunkSize, chunkHeight, chunkSize - 1];
        float noiseScale = 0.06f;
        public float threshold = 0.7f;
        public bool sixPointActive, sixFaceCancel;
        public bool fivePointActive, fiveFaceCancel;
        public bool fourHoriSquareActive, fourHoriFaceReverse;
        public bool fourVertSquareActive, fourVertFaceReverse;
        public bool fourTetraActive, fourTetraFaceCancel;
        public bool threePointActive, threeFaceReverse;
        public bool thirdDiagonalActive, thirdDiagonalFaceReverse;
        public bool meshRecalculate;
        #endregion

        #region Calculated Lists
        //Type used for face lookup
        class InnerFaceKey : IEquatable<InnerFaceKey>
        {
            int tetraID, failVert;
            public InnerFaceKey(int ID, int fail) { tetraID = ID; failVert = fail; }
            public bool Equals(InnerFaceKey other) { return other.tetraID == tetraID && other.failVert == failVert; }
            public override int GetHashCode() { return tetraID.GetHashCode() ^ failVert.GetHashCode(); }
        }
        //public static Vector3[] tetraPoints = { new Vector3(0, 0, 0), new Vector3(1.5f, 0, 1), new Vector3(0, 0, 2), new Vector3(1.5f, 0, -1), new Vector3(.5f, -1f / 3f, 1), new Vector3(1, 1f / 3f, 0)};
        public static Vector3[] tetraPoints = { new Vector3(0, 0, 0), new Vector3(Mathf.Sqrt(3), 0, 1),
            new Vector3(0, 0, 2), new Vector3(Mathf.Sqrt(3), 0, -1),
            new Vector3(Mathf.Sqrt(3)-(2*Mathf.Sqrt(3) / 3), -2 * Mathf.Sqrt(1-Mathf.Sqrt(3)/3), 1),
            new Vector3(2 * Mathf.Sqrt(3) / 3, 2 * Mathf.Sqrt(1 - Mathf.Sqrt(3) / 3), 0) };
#endregion

        void Start()
        {
            GenerateMesh(chunkSize);
        }

        #region On Draw
        /// <summary>
        /// Method called when object is selected
        /// </summary>
        void OnDrawGizmosSelected ()
        {
            for(int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkHeight; j++)
                {
                    for (int k = 0; k < chunkSize - 1; k++)
                    {
                        if (hits[i, j, k])
                        {
                            Vector3 vert = HexToPos(new WorldPos(i, j, k));
                            //vert = new Vector3(vert.x * Mathf.Sqrt(3) / 1.5f, vert.y * 2, vert.z);
                            Gizmos.color = Color.gray;
                            Gizmos.DrawSphere(vert, .2f);
                            if (world.show)
                            {
                                Vector3 dir = Procedural.Noise.noiseMethods[0][2](vert, noiseScale).derivative.normalized;
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawRay(vert, dir);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Face Construction
        /// <summary>
        /// Creates mesh
        /// </summary>
        /// <param name="wid">Width of points to be generated</param>
        void GenerateMesh(int wid)
        {
            List<Vector3[]> verts = new List<Vector3[]>();

            for (int z = 0; z < wid - 1; z++)
            {
                verts.Add(new Vector3[wid]);
                for (int x = 0; x < wid; x++)
                {
                    Vector3 currentPoint = new Vector3(x + posOffset.x, 0, z + posOffset.y);

                    int offset = z % 2;
                    if (offset == 1)
                        currentPoint.x -= (1-tetraPoints[4].x/tetraPoints[5].x);

                    float tempH = Mathf.Round(currentPoint.y);
                    currentPoint.y += (2 * x + (offset == 1 ? 2 : 3)) % 3;
                    currentPoint.y = (tempH - Mathf.Round(currentPoint.y)) / 3 + tempH;
                    currentPoint.y *= (tetraPoints[5].y)*3/2;
                    verts[z][x] = currentPoint;
                }
            }

            Vector3[] uVerts = new Vector3[wid * (wid - 1)];
            int i = 0;
            foreach (Vector3[] v in verts)
            {
                v.CopyTo(uVerts, i * wid);
                i++;
            }
            MeshFromPoints(uVerts);
        }

        /// <summary>
        /// Creates a mesh for the chunk based on a set of points it was handed
        /// </summary>
        /// <param name="basePoints">Points to check</param>
        public void MeshFromPoints(Vector3[] basePoints)
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            //Point Reading
            foreach (var basePoint in basePoints)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    
                    Vector3 center = new Vector3(basePoint.x, basePoint.y + y* (tetraPoints[5].y) * 3 / 2, basePoint.z);
                    if (Land(center) && GradientCheck(center))
                    {
                        //hits[PosToHex(center).x, PosToHex(center).y, PosToHex(center).z] = true;
                        if (world.pointLoc)
                        {
                            CreatePoint(center);
                        }
                    }
                }
            }

            //Face Construction
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    for (int z = 0; z < chunkSize - 1; z++)
                    {
                        //FaceBuilder(HexToPos(new WorldPos(x, y, z)), ref verts, ref tris, ref normals);
                    }
                }
            }

            //Mesh Procedure
            MeshFilter filter = gameObject.GetComponent<MeshFilter>();
            filter.mesh.Clear();
            filter.mesh.vertices = verts.ToArray();
            filter.mesh.triangles = tris.ToArray();
            filter.mesh.normals = normals.ToArray();
            filter.mesh.RecalculateBounds();
            if (meshRecalculate) { filter.mesh.RecalculateNormals(); }
        }

        /*
        /// <summary>
        /// Constructs adjacent faces at a point. All faces are withing two adjacent tetrahedra
        /// </summary>
        /// <param name="center"></param>
        /// <param name="verts"></param>
        /// <param name="tris"></param>
        void TetraFaceBuilder(Vector3 center, ref List<Vector3> verts, ref List<int> tris, ref List<Vector3> normals)
        {
            //If point isn't existent
            bool pullout = true;
            bool[] checks = new bool[6];
            
            //List<Vector3> vertTemp = new List<Vector3>();
            //Checks hits for each point
            for (int index = 0; index < 6; index++)
            {
                if (CheckHit(center + tetraPoints[index]))
                {
                    checks[index] = true;
                    pullout = false;
                }
                else
                    checks[index] = false;
                
                Vector3 vert = center + tetraPoints[index];
            }

            if (pullout)
                return;


            //Checks individual tetrahedra
            for (int tetra = 0; tetra < 4; tetra++)
            {
                int pointVertCount = verts.Count;
                List<Vector3> vertTemp = new List<Vector3>();
                int fail = -1;
                //Checks at each point on the tetrahedron
                for (int j = 0; j < 4; j++)
                {
                    if (checks[tetras[tetra][j]])
                    {
                        Vector3 vert = center + tetraPoints[tetras[tetra][j]];
                        vertTemp.Add(new Vector3(vert.x * Mathf.Sqrt(3) / 1.5f, vert.y * 2, vert.z));
                    }
                    else
                        fail = tetras[tetra][j];
                }
                //Full tetrahedron build
                if (vertTemp.Count == 4)
                {
                    List<int>[] triTemp;
                    tetraIDs.TryGetValue(tetra, out triTemp);
                    for (int face = 0; face < 4; face++)
                    {
                        int tetraVertCount = verts.Count;
                        if (!TriNormCheck(center, tetraPoints[tetras[tetra][face]]))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                verts.Add(vertTemp[triTemp[face][i]]);
                                tris.Add(i + tetraVertCount);
                                normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                            }
                        }
                    }
                }
                //Single face build
                else if (vertTemp.Count == 3)
                {
                    foreach (var vert in vertTemp)
                    {
                        verts.Add(vert);
                        normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                    }
                    List<int> triTemp;
                    if (!innerFaceIDs.TryGetValue(new InnerFaceKey(tetra, fail), out triTemp))
                        print(tetra + ", " + fail);
                    if(TriNormCheck(center, tetraPoints[fail]))
                        for (int tri = 0; tri < 3; tri++) { tris.Add(triTemp.ToArray()[tri] + pointVertCount); }
                    else
                        for (int tri = 2; tri >= 0; tri--) { tris.Add(triTemp.ToArray()[tri] + pointVertCount); }
                }
            }
            //Square Builds
        }
        */


        void FaceBuilder(Vector3 center, ref List<Vector3> verts, ref List<int> tris, ref List<Vector3> normals)
        {
            List<Vector3> vertTemp = new List<Vector3>();
            List<int> vertFail = new List<int>();
            List<int> vertSuccess = new List<int>();
            int vertCount = verts.Count;

            for (int i = 0; i < 6; i++)
            {
                Vector3 vert = center + tetraPoints[i];
                if (CheckHit(vert))
                {
                    vertTemp.Add(vert);// new Vector3(vert.x * Mathf.Sqrt(3) / 1.5f, vert.y * 2, vert.z));
                    vertSuccess.Add(i);
                }
                else
                    vertFail.Add(i);
            }
            
            if(vertTemp.Count == 6 && sixPointActive)
            {
                //Octahedron

                for (int face = 0, faceIndex = 0; face < 8; face++)
                {
                    int[] triTemp = { (face / 2) % 2 == 0 ? 0 : 1, face % 2 == 0 ? 4 : 5, (face / 4) % 2 == 0 ? 2 : 3 };
                    if (face == 1 || face == 2 || face == 4 || face == 7)
                        Array.Reverse(triTemp);
                    Vector3 faceVec = Vector3.Cross(tetraPoints[triTemp[1]] - tetraPoints[triTemp[0]], tetraPoints[triTemp[2]] - tetraPoints[triTemp[0]]);
                    if (TriNormCheck(center, faceVec.normalized))
                    {
                        int i = 0;
                        foreach (int tri in triTemp)
                        {
                            verts.Add(vertTemp[tri]);
                            tris.Add(vertCount + 3 * faceIndex + i);
                            normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
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
                        Vector3 faceVec = Vector3.Cross(tetraPoints[triTemp[1]] - tetraPoints[triTemp[0]], tetraPoints[triTemp[2]] - tetraPoints[triTemp[0]]);
                        if (TriNormCheck(center, faceVec.normalized))
                        {
                            int i = 0;
                            foreach (int tri in triTemp)
                            {
                                verts.Add(vertTemp[vertFail[0] <= tri ? tri - 1 : tri]);
                                tris.Add(vertCount + 3 * faceIndex + i);
                                normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                                i++;
                            }
                            faceIndex++;
                        }
                    }
                }
                vertCount = verts.Count;
                vertTemp.Remove(vertFail[0]%2==0?vertTemp[vertFailOpposite-1]:vertTemp[vertFailOpposite]);
                if (vertFail[0] == 0 || vertFail[0] == 3 || vertFail[0] == 4)
                {
                    Vector3 faceVec = tetraPoints[vertFail[0]];
                    if (TriNormCheck(center, faceVec.normalized))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            verts.Add(vertTemp[i]);
                            tris.Add(vertCount + i);
                            normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            verts.Add(vertTemp[i == 2 ? 3 : i]);
                            tris.Add(vertCount + 5 - i);
                            normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        }
                    }
                }
                else
                {
                    Vector3 faceVec = tetraPoints[vertFail[0]];
                    if (TriNormCheck(center, faceVec.normalized))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            verts.Add(vertTemp[i]);
                            tris.Add(vertCount + 2 - i);
                            normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            verts.Add(vertTemp[i == 2 ? 3 : i]);
                            tris.Add(vertCount + 3 + i);
                            normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        }
                    }
                }
            }
            
            if(vertSuccess.Count == 4)
            {
                if (vertFail[0] == 4 && vertFail[1] == 5 && fourHoriSquareActive)
                {
                    //Horizontal Square
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i]);
                        normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        Vector3 faceVec = Vector3.Cross(tetraPoints[1] - tetraPoints[0], tetraPoints[2] - tetraPoints[0]);
                        if (TriNormCheck(center, faceVec.normalized))
                            tris.Add(vertCount + i);
                        else
                            tris.Add(vertCount + 2 - i);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        //int iTemp = i == 2 ? 3 : i;
                        verts.Add(vertTemp[i]);
                        normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        Vector3 faceVec = Vector3.Cross(tetraPoints[1] - tetraPoints[0], tetraPoints[3] - tetraPoints[0]);
                        if (TriNormCheck(center, faceVec.normalized))
                            tris.Add(vertCount + 3 + i);
                        else
                            tris.Add(vertCount + 5 - i);
                    }
                    //Can't find Horizontal Square
                }
                else if(vertFail[0] == 2 && vertFail[1] == 3 && fourVertSquareActive)
                {
                    
                    //Point Slanted Square
                    
                    for (int i = 0; i < 3; i++)
                    {
                        verts.Add(vertTemp[i]);
                        normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        Vector3 faceVec = Vector3.Cross(tetraPoints[1] - tetraPoints[0], tetraPoints[4] - tetraPoints[0]);
                        if (TriNormCheck(center, faceVec.normalized))
                            tris.Add(vertCount + i);
                        else
                            tris.Add(vertCount + 2 - i);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int iTemp = i == 2 ? 3 : i;
                        verts.Add(vertTemp[iTemp]);
                        normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                        Vector3 faceVec = Vector3.Cross(tetraPoints[1] - tetraPoints[0], tetraPoints[5] - tetraPoints[0]);
                        if (TriNormCheck(center, faceVec.normalized))
                            tris.Add(vertCount + 3 + i);
                        else
                            tris.Add(vertCount + 5 - i);
                    }
                }
                else if(vertFail[0] == 0 && vertFail[1] == 1)
                {
                    print("Vert Part 2");
                }
                else if(fourTetraActive)
                {
                    //Tetrahedron
                    int corner = vertSuccess[2];
                    int point = vertSuccess[3];
                    int[] triTemp = { 0, corner, 1, 0, 1, point, 0, point, corner, 1, corner, point};
                    int i = 0;
                    for (int face = 0; face < 4; face++)
                    {
                        Vector3 faceVec = Vector3.Cross(tetraPoints[triTemp[1 + 3 * face]] - tetraPoints[triTemp[3 * face]], tetraPoints[triTemp[2 + 3 * face]] - tetraPoints[triTemp[3 * face]]);
                        if (TriNormCheck(center, faceVec.normalized))
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (triTemp[3 * face + j] == corner)
                                    verts.Add(vertTemp[2]);
                                else if (triTemp[3 * face + j] == point)
                                    verts.Add(vertTemp[3]);
                                else
                                    verts.Add(vertTemp[triTemp[3 * face + j]]);
                                tris.Add(vertCount + i);
                                normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                                i++;
                            }
                        }
                    }
                }
              
            }
            
            if(vertTemp.Count == 3 && threePointActive)
            {
                //Triangle
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                    Vector3 faceVec = Vector3.Cross(tetraPoints[vertSuccess[1]] - tetraPoints[vertSuccess[0]], tetraPoints[vertSuccess[2]] - tetraPoints[vertSuccess[0]]);
                    if (TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + i);
                    else
                        tris.Add(vertCount + 2 - i);
                }
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(vertTemp[i]);
                    normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                    Vector3 faceVec = Vector3.Cross(tetraPoints[vertSuccess[1]] - tetraPoints[vertSuccess[0]], tetraPoints[vertSuccess[2]] - tetraPoints[vertSuccess[0]]);
                    if (!TriNormCheck(center, faceVec.normalized))
                        tris.Add(vertCount + i);
                    else
                        tris.Add(vertCount + 2 - i);
                }
            }   
            //Third Slant Face
            if(CheckHit(center) && CheckHit(center + tetraPoints[1]) && CheckHit(center - tetraPoints[3] + tetraPoints[5]) && CheckHit(center + tetraPoints[2] + tetraPoints[5]) && thirdDiagonalActive)
            {
                vertCount = verts.Count;
                vertTemp.Clear();
                vertTemp.Add(center);
                vertTemp.Add(center - tetraPoints[3] + tetraPoints[5]);
                vertTemp.Add(center + tetraPoints[2] + tetraPoints[5]);
                vertTemp.Add(center);
                vertTemp.Add(center + tetraPoints[2] + tetraPoints[5]);
                vertTemp.Add(center + tetraPoints[1]);
                for (int i = 0; i < 6; i++)
                {
                    verts.Add(new Vector3(vertTemp[i].x * Mathf.Sqrt(3) / 1.5f, vertTemp[i].y * 2, vertTemp[i].z));
                    tris.Add(vertCount + i);
                    normals.Add(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative.normalized);
                }
            }
        }
        #endregion

        #region Checks
        /// <summary>
        /// Checks if a point is on the edge of a surface using IVT and gradients
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>Boolean</returns>
        bool GradientCheck(Vector3 point)
        {
            Vector3 normal = Procedural.Noise.noiseMethods[0][2](point, noiseScale).derivative.normalized * 2f;
            if (GetNoise(point + normal, noiseScale) > threshold && GetNoise(point - normal, noiseScale) < threshold)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if the point is within a solid
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>Boolean</returns>
        bool Land(Vector3 point)
        {
            //print(GetNoise(point, noiseScale));
            return GetNoise(point, noiseScale) < threshold;
        }

        /// <summary>
        /// Checks if a triangle faces the same direction as the noise
        /// </summary>
        /// <param name="center">Point to check</param>
        /// <param name="normal">Normal to check</param>
        /// <returns>Boolean</returns>
        bool TriNormCheck(Vector3 center, Vector3 normal)
        {
            return 90 > Vector3.Angle(Procedural.Noise.noiseMethods[0][2](center, noiseScale).derivative, normal);
        }

        /// <summary>
        /// Returns value of 3D noise at a point
        /// </summary>
        /// <param name="pos">Point to check</param>
        /// <param name="scale">Size of the waves</param>
        /// <returns>Value of the noise</returns>
        public static float GetNoise(Vector3 pos, float scale)
        {
            return Procedural.Noise.noiseMethods[0][2](pos, scale).value * 20 + 10;
        }

        /// <summary>
        /// Finds if this point is in the checks array
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>Boolean</returns>
        bool CheckHit(Vector3 point)
        {
            bool output;
            try { output = hits[PosToHex(point).x, PosToHex(point).y, PosToHex(point).z]; }
            catch { output = false; }
            return output;
        }
        #endregion

        #region Conversions
        /// <summary>
        /// Converts from World Position to Hex Coordinates
        /// </summary>
        /// <param name="point">World Position</param>
        /// <returns>Hex Coordinate</returns>
        public WorldPos PosToHex (Vector3 point)
        {
            WorldPos output = new WorldPos(Mathf.CeilToInt(point.x), Mathf.CeilToInt(point.y), (int)point.z);
            output.x -= (int)posOffset.x;
            output.z -= (int)posOffset.y;
            return output;
        }

        /// <summary>
        /// Converts from Hex Coordinate to World Position
        /// </summary>
        /// <param name="point">Hex Coordinate</param>
        /// <returns>World Position</returns>
        public Vector3 HexToPos (WorldPos point)
        {
            point.x += (int)posOffset.x;
            point.z += (int)posOffset.y;
            float x = point.z % 2 == 0 ? point.x : point.x - Mathf.Sqrt(3);
            float y = point.y - Mathf.Abs((((point.x + Mathf.Abs(point.z % 2f) - (int)posOffset.x - 15)) % 3f) / 3f);
            Vector3 output = new Vector3(x,y,point.z);
            return output;
        }
        #endregion

        #region Debug
        void CreatePoint(Vector3 location)
        {
            GameObject copy = Instantiate(dot, new Vector3(location.x * Mathf.Sqrt(3) / 1.5f, location.y * 2, location.z), new Quaternion(0, 0, 0, 0)) as GameObject;
            copy.transform.parent = gameObject.transform;
        }

        public void FaceBuilderCheck(Vector3 center)
        {
            List<Vector3> vertTemp = new List<Vector3>();
            List<int> vertFail = new List<int>();
            List<int> vertSuccess = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                Vector3 vert = center + tetraPoints[i];
                print(vert);
                if (CheckHit(vert))
                {
                    vertTemp.Add(vert);//new Vector3(vert.x * Mathf.Sqrt(3) / 1.5f, vert.y * 2, vert.z));
                    vertSuccess.Add(i);
                }
                else
                    vertFail.Add(i);
            }
            print(vertTemp.Count);
            switch (vertTemp.Count)
            {
                case 6:
                    print(center + "= " + "Octahedron");
                    break;

                case 5:
                    print(center + "= " + "Rectangular Prism");
                    break;

                case 4:
                    if (vertFail[0] == 4 && vertFail[1] == 5)
                        print(center + "= " + "Horizontal Square");
                    else if (vertFail[0] == 2 && vertFail[1] == 3)
                        print(center + "= " + "Vertical Square");
                    else if (vertFail[0] == 0 && vertFail[1] == 1)
                        print(center + "= " + "Vertical Square 2");
                    else
                        print(center + "= " + "Tetrahedron");
                    break;

                case 3:
                    print(center + "= " + "Triangle");
                    break;

                default:
                    break;
            }

            if (CheckHit(center) && CheckHit(center + tetraPoints[1]) && CheckHit(center - tetraPoints[3] + tetraPoints[5]) && CheckHit(center + tetraPoints[2] + tetraPoints[5]))
                print(center + "= " + "Third Diagonal");
        }
        #endregion
    }
}
