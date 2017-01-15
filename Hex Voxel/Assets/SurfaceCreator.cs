using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceCreator : MonoBehaviour
{
    [Range(1, 200)]
    public int res = 10;
    int currentRes;

    public Vector3 offset;

    //Length of side relative to normal tile basically anything over 1's size needs to be half of the number tiles you want covered
    public Vector3 size;

    public Vector3 rotation;

    [Range(0f, 2f)]
    public float strength = 1f;

    public float frequency = 1f;

    [Range(1, 8)]
    public int octaves = 1;

    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Range(1, 3)]
    public int dimensions = 3;

    public Procedural.NoiseMethodType type;

    public Gradient coloring;

    public bool coloringForStrength = true;

    public bool damping = false;

    public bool showNormals;

    public bool analyticalDerivatives;

    [Range(0f, 1f)]
    public float flatBottom;

    public GameObject DecidousTree;
    public GameObject ConiferTree;
    public GameObject[] rocks;
    public GameObject grass;

    Mesh mesh;
    Vector3[] vertices;
    Color[] colors;
    Vector3[] normals;

    void OnEnable ()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Surface Mesh";
            GetComponent<MeshFilter>().mesh = mesh;
            if (GetComponent<MeshCollider>() != null)
                GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        Refresh();
    }

    void OnDrawGizmosSelected()
    {
        if (showNormals && vertices != null)
        {
            Gizmos.color = Color.yellow;
            for (int v = 0; v < vertices.Length; v++)
            {
                Gizmos.DrawRay(vertices[v],normals[v] / res);
            }
        }
    }

    public void Refresh()
    {
        if (res != currentRes)
            CreateGrid();        
        Quaternion q = Quaternion.Euler(rotation);
        Quaternion qInv = Quaternion.Inverse(q);
        Vector3 point00 = q * transform.TransformPoint(new Vector3(-0.5f, 0f, -0.5f)) + offset;
        Vector3 point10 = q * transform.TransformPoint(new Vector3( 0.5f, 0f, -0.5f)) + offset;
        Vector3 point01 = q * transform.TransformPoint(new Vector3(-0.5f, 0f, 0.5f)) + offset;
        Vector3 point11 = q * transform.TransformPoint(new Vector3( 0.5f, 0f, 0.5f)) + offset;

        float amplitude = damping ? strength / frequency : strength;
        Procedural.NoiseMethod method = Procedural.Noise.noiseMethods[(int)type][dimensions - 1];
        int n = 0;
        for (int z = 0; z <= res; z++)
        {
            Vector3 point0 = Vector3.Lerp(point00, point01, (float)z / res);
            Vector3 point1 = Vector3.Lerp(point10, point11, (float)z / res);
            for (int x = 0; x <= res; x++, n++)
            {
                Vector3 point = Vector3.Lerp(point0, point1, (float)x / res);
                NoiseSample sample = Procedural.Noise.Sum(method, Vector3.Scale(point, size), frequency, octaves, lacunarity, persistence);
                sample = type == Procedural.NoiseMethodType.Value ? (sample - 0.5f) : (sample * 0.5f);
                sample.value = sample.value < flatBottom - .5f ? flatBottom - .5f : sample.value;
                if (coloringForStrength)
                {
                    colors[n] = coloring.Evaluate(sample.value + 0.5f);
                    sample *= amplitude;
                }
                else
                {
                    sample *= amplitude;
                    colors[n] = coloring.Evaluate(sample.value + 0.5f);
                }
                if (Procedural.Noise.Perlin3D(point, 50).value > .4f && sample.value>-4f && sample.value<.5f && sample.derivative.magnitude < 1)
                {
                    float ranAngle = Random.Range(0, 2 * Mathf.PI);
                    float treeControl = Random.Range(0, 2);
                    Quaternion ranRot = Quaternion.LookRotation(new Vector3(Mathf.Sin(ranAngle),0,Mathf.Cos(ranAngle)), Vector3.up);
                    GameObject clone = Instantiate(treeControl<.5f?DecidousTree:ConiferTree, new Vector3(point.x,sample.value,point.z), ranRot, transform) as GameObject;
                    clone.transform.localScale = new Vector3(.005f, .005f, .005f);
                }
                if (Procedural.Noise.Perlin3D(new Vector3(point.x,100,point.z), 3).value > .6f && sample.value > -4f)
                {
                    float ranAngle = Random.Range(0, 2 * Mathf.PI);
                    float rockControl = Random.Range(0, 4);
                    Quaternion ranRot = Random.rotation;
                    GameObject clone = Instantiate(rocks[Mathf.FloorToInt(rockControl)], new Vector3(point.x, sample.value, point.z), ranRot, transform) as GameObject;
                    clone.transform.localScale = new Vector3(Random.Range(.2f,2), Random.Range(.2f, 2), Random.Range(.2f, 2));
                }
                vertices[n].y = sample.value;
                sample.derivative = qInv * sample.derivative;
                if (analyticalDerivatives)
                    normals[n] = new Vector3(-sample.derivative.x, 1f, -sample.derivative.y).normalized;
            }
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (!analyticalDerivatives)
            CalculateNormals();
    }
    
    public void CreateGrid()
    {
        currentRes = res;
        mesh.Clear();
        vertices = new Vector3[(res + 1) * (res + 1)];
        colors = new Color[vertices.Length];
        normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        float stepSize = 1f / res;
        for (int v = 0, z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++, v++)
            {
                vertices[v] = new Vector3(x * stepSize - 0.5f, 0f, z * stepSize - 0.5f);
                colors[v] = Color.black;
                normals[v] = Vector3.up;
                uv[v] = new Vector2(x * stepSize, z * stepSize);
            }
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.normals = normals;
        mesh.uv = uv;

        int[] triangles = new int[res * res * 6];
        for (int t = 0, v = 0, y = 0; y < res; y++, v++)
        {
            for (int x = 0; x < res; x++, v++, t += 6)
            {
                triangles[t] = v;
                triangles[t + 1] = v + res + 1;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + res + 1;
                triangles[t + 5] = v + res + 2;
            }
        }
        mesh.triangles = triangles;
    }

    void CalculateNormals ()
    {
        int v = 0;
        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++, v++)
            {
                normals[v] = new Vector3(GetXDerivative(x, z), 0f, GetZDerivative(x, z)).normalized;
            }
        }
    }

    float GetXDerivative (int x, int z)
    {
        int rowOffset = z * (res + 1);
        float left, right, scale;
        if (x > 0)
        {
            left = vertices[rowOffset + x - 1].y;
            if (x < res)
            {
                right = vertices[rowOffset + x + 1].y;
                scale = res / 2;
            }
            else
            {
                right = vertices[rowOffset + x].y;
                scale = res;
            }
        }
        else
        {
            left = vertices[rowOffset + x].y;
            right = vertices[rowOffset + x + 1].y;
            scale = res;
        }
        return (right - left) * scale;
    }

    float GetZDerivative(int x, int z)
    {
        int rowLength = res + 1;
        float back, forward, scale;
        if (z > 0)
        {
            back = vertices[(z - 1) * rowLength + x].y;
            if (z < res)
            {
                forward = vertices[(z + 1) * rowLength + x].y;
                scale = 0.5f * res;
            }
            else
            {
                forward = vertices[z * rowLength + x].y;
                scale = res;
            }
        }
        else
        {
            back = vertices[z * rowLength + x].y;
            forward = vertices[(z + 1) * rowLength + x].y;
            scale = res;
        }
        return (forward - back) * scale;
    }
}

public struct NoiseSample
{
    public float value;
    public Vector3 derivative;

    public static NoiseSample operator +(NoiseSample a, NoiseSample b)
    {
        a.value += b.value;
        a.derivative += b.derivative;
        return a;
    }

    public static NoiseSample operator +(float a, NoiseSample b)
    {
        b.value += a;
        return b;
    }

    public static NoiseSample operator +(NoiseSample a, float b)
    {
        a.value += b;
        return a;
    }

    public static NoiseSample operator -(NoiseSample a, NoiseSample b)
    {
        a.value -= b.value;
        a.derivative -= b.derivative;
        return a;
    }

    public static NoiseSample operator -(float a, NoiseSample b)
    {
        b.value = a - b.value;
        b.derivative = -b.derivative;
        return b;
    }

    public static NoiseSample operator -(NoiseSample a, float b)
    {
        a.value -= b;
        return a;
    }

    public static NoiseSample operator *(NoiseSample a, NoiseSample b)
    {
        a.derivative = a.derivative * b.value + b.derivative * a.value;
        a.value *= b.value;
        return a;
    }

    public static NoiseSample operator *(float a, NoiseSample b)
    {
        b.value *= a;
        b.derivative *= a;
        return b;
    }

    public static NoiseSample operator *(NoiseSample a, float b)
    {
        a.value *= b;
        a.derivative *= b;
        return a;
    }
}
