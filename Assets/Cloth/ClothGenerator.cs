using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothGenerator : MonoBehaviour
{
    MeshFilter filter;
#pragma warning disable CS0108 // 'ClothGenerator.renderer' hides inherited member 'Component.renderer'. Use the new keyword if hiding was intended.
    MeshRenderer renderer;
#pragma warning restore CS0108 // 'ClothGenerator.renderer' hides inherited member 'Component.renderer'. Use the new keyword if hiding was intended.

    Mesh mesh;

    public int width;
    public int height;

    Vector3[] verticies;
    int[] triangles;


    public void Initialize()
    {
        mesh = new Mesh();
        filter = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();
        filter.mesh = mesh;

        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        verticies = new Vector3[(width + 1) * (height + 1)];

        for (int i = 0, z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                verticies[i] = new Vector3(x, 0, z);
                i++;
            }
        }
    }

    void CalculateTris()
    {
        triangles = new int[width*height*6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;
                vert++;
                tris += 6;
            }
            vert++;
        }
     
    }

    public void UpdateMesh()
    {
        CalculateTris();

        mesh.Clear();

        mesh.vertices = verticies;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }
}
