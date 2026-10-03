using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelTerrain : MonoBehaviour
{
    public int width = 32;
    public int height = 16;
    public int depth = 32;

    public float voxelSize = 0.2f;

    [Range(0.01f, 0.5f)]
    public float noiseScale = 0.1f;

    [Range(1f, 15f)]
    public float noiseHeight = 5f;

    byte[,,] blocks;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    int vertexIndex = 0;

    void Start()
    {
        blocks = new byte[width, height, depth];
        PopulateData();
        BuildMesh();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            RemoveBlock();
        }
    }

    void PopulateData()
    {
        for(int x = 0; x < width; x++)
        {
            for(int z = 0; z < depth; z++)
            {
                float xNoise = x * noiseScale;
                float zNoise = z * noiseScale;
                float noiseValue = Mathf.PerlinNoise(xNoise, zNoise);
                int surface = Mathf.Clamp(Mathf.FloorToInt(noiseValue * noiseHeight), 0, height - 1);

                for(int y = 0; y < height; y++)
                {
                    if(y <= surface)
                    {
                        blocks[x, y, z] = (byte)(y == surface ? 2 : 1);
                    }
                }
            }
        }
    }

    void BuildMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        vertexIndex = 0;

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                for(int z = 0; z < depth; z++)
                {
                    if(blocks[x, y, z] != 0)
                    {
                        DrawVoxel(x, y, z);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void DrawVoxel(int x, int y, int z)
    {
        byte blockType = blocks[x, y, z];

        if(HasAir(x, y + 1, z)) AddFace(x, y, z, Vector3.up, blockType);
        if(HasAir(x, y - 1, z)) AddFace(x, y, z, Vector3.down, blockType);
        if(HasAir(x, y, z + 1)) AddFace(x, y, z, Vector3.forward, blockType);
        if(HasAir(x, y, z - 1)) AddFace(x, y, z, Vector3.back, blockType);
        if(HasAir(x + 1, y, z)) AddFace(x, y, z, Vector3.right, blockType);
        if(HasAir(x - 1, y, z)) AddFace(x, y, z, Vector3.left, blockType);
    }

    bool HasAir(int x, int y, int z)
    {
        if(x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth) return true;
        return blocks[x, y, z] == 0;
    }

    void AddFace(int x, int y, int z, Vector3 direction, byte blockType)
    {
        Vector3 position = new Vector3(x, y, z) * voxelSize;
        float s = voxelSize;

        if(direction == Vector3.up)
        {
            vertices.Add(position + new Vector3(0, s, 0));
            vertices.Add(position + new Vector3(0, s, s));
            vertices.Add(position + new Vector3(s, s, s));
            vertices.Add(position + new Vector3(s, s, 0));
        }
        else if(direction == Vector3.down)
        {
            vertices.Add(position + new Vector3(0, 0, s));
            vertices.Add(position + new Vector3(0, 0, 0));
            vertices.Add(position + new Vector3(s, 0, 0));
            vertices.Add(position + new Vector3(s, 0, s));
        }
        else if(direction == Vector3.forward)
        {
            vertices.Add(position + new Vector3(s, 0, s));
            vertices.Add(position + new Vector3(s, s, s));
            vertices.Add(position + new Vector3(0, s, s));
            vertices.Add(position + new Vector3(0, 0, s));
        }
        else if(direction == Vector3.back)
        {
            vertices.Add(position + new Vector3(0, 0, 0));
            vertices.Add(position + new Vector3(0, s, 0));
            vertices.Add(position + new Vector3(s, s, 0));
            vertices.Add(position + new Vector3(s, 0, 0));
        }
        else if(direction == Vector3.right)
        {
            vertices.Add(position + new Vector3(s, 0, 0));
            vertices.Add(position + new Vector3(s, s, 0));
            vertices.Add(position + new Vector3(s, s, s));
            vertices.Add(position + new Vector3(s, 0, s));
        }
        else if(direction == Vector3.left)
        {
            vertices.Add(position + new Vector3(0, 0, s));
            vertices.Add(position + new Vector3(0, s, s));
            vertices.Add(position + new Vector3(0, s, 0));
            vertices.Add(position + new Vector3(0, 0, 0));
        }

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);

        AddUVs(blockType);
        vertexIndex += 4;
    }

    void AddUVs(byte blockType)
    {
        float xMin = blockType == 1 ? 0f : 0.5f;
        float xMax = blockType == 1 ? 0.5f : 1f;

        uvs.Add(new Vector2(xMin, 0));
        uvs.Add(new Vector2(xMin, 1));
        uvs.Add(new Vector2(xMax, 1));
        uvs.Add(new Vector2(xMax, 0));
    }

    void RemoveBlock()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, 10f))
        {
            Vector3 pointInCube = hit.point - (hit.normal * 0.1f);

            int x = Mathf.FloorToInt(pointInCube.x);
            int y = Mathf.FloorToInt(pointInCube.y);
            int z = Mathf.FloorToInt(pointInCube.z);

            if(x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
            {
                blocks[x, y, z] = 0;
                BuildMesh();
            }
        }
    }
}