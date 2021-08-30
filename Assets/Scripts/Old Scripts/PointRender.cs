using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointRender : MonoBehaviour
{
    private Mesh mesh;
    [SerializeField]
    Vector3 dimensions;

    int size;

    // Use this for initialization
    void Start()
    {
        size = (int)(dimensions.x * dimensions.y * dimensions.z);
        mesh = new Mesh();

        GetComponent<MeshFilter>().mesh = mesh;
        CreateMesh();
    }

    void CreateMesh()
    {
        Vector3[] points = new Vector3[size];
        int[] indecies = new int[size];
        Color[] colors = new Color[size];

        int x = 0, y = 0, z = 0;
        for (int i = 0; i < size; i++)
        {
            points[i] = new Vector3(x/dimensions.x, y/dimensions.y, z/dimensions.z);
            indecies[i] = i;
            colors[i] = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            x++;
            if (x % dimensions.x == 0)
            {
                x = 0;
                y++;
                if (y % dimensions.y == 0)
                {
                    y = 0;
                    z++;
                    if (z % dimensions.z == 0)
                    {
                        z = 0;
                    }
                }
            }
        }

        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indecies, MeshTopology.Points, 0);

    }
}
