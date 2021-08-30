using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulator : MonoBehaviour
{
    public ParticleSpatialVolume psv;
    public float particleInfluence;
    public float particleCount;

    public Vector3Int MinBound;
    public Vector3Int MaxBound;

    public static List<Particle> particles = new List<Particle>();
    void Start()
    {
        psv.Init((MinBound,MaxBound));

        for(int i =0;i<particleCount;i++)
        {
            var unitPos = Random.insideUnitSphere;
            var randPos = new Vector3(unitPos.x*psv.BoundDims.x,unitPos.y*psv.BoundDims.y,unitPos.z*psv.BoundDims.z);
            particles.Add(new Particle()
            {
                position = i > 0 ? randPos : Vector3.zero,
                key = "",
                radius = particleInfluence
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        psv.Step(particleInfluence);
    }

    private void OnDrawGizmos()
    {
        foreach(Particle p in particles)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(p.position,1);
        }
    }
}
