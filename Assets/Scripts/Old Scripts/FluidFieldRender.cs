using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class FluidFieldRender : MonoBehaviour
{

    [SerializeField]
    private GameObject particle;

    [SerializeField]
    public Vector3 resolution = new Vector3(5, 5, 5);

    [SerializeField]
    public Vector3 dimensions = new Vector3(5, 5, 5);

    [SerializeField]
    private Mesh mesh;

    [SerializeField]
    private Material material;

    private bool displayDensities;
    private bool displayParticles;

    private FluidParticle[] particles;

    public static int size;

    // Start is called before the first frame update
    public void Initialze()
    {
        size = (int)((resolution.x) * (resolution.y) * (resolution.z));
        particles = new FluidParticle[size];
        int x = 0, y = 0, z = 0;
        for (int i = 0; i < size; i++)
        {
            particles[i] = new FluidParticle();
            particles[i].position = new Vector3(x, y, z);
            x++;
            if (x % resolution.x == 0)
            {
                x = 0;
                y++;
                if (y % resolution.y == 0)
                {
                    y = 0;
                    z++;
                    if (z % resolution.z == 0)
                    {
                        z = 0;
                    }
                }
            }
        }
    }
    public FluidParticle[] Particles
    {
        get { return particles; }
        set { particles = value; }
    }

    public Vector3 Dimensions
    {
        get { return dimensions; }
        set { dimensions = value; }
    }

    public Vector3 Resolution
    {
        get { return resolution; }
        set { resolution = value; }
    }

    public float Size
    {
        get { return size; }
    }

    public bool DisplayDensities
    {
        get { return displayDensities; }
        set { displayDensities = value; }
    }
    public bool DisplayParticles
    {
        get { return displayParticles; }
        set { displayParticles = value; }
    }

    public Mesh Mesh
    {
        get { return mesh; }
    }

    public Material Material
    {
        get { return material; }
    }

}
