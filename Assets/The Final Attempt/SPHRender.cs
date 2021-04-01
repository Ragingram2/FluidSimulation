using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHRender : MonoBehaviour
{

    [SerializeField]
    private Transform parent;
    [SerializeField]
    private GameObject sphere;
    [SerializeField]
    private GameObject source;
    [SerializeField]
    private Vector3 gridCenter;
    [SerializeField]
    private Vector3 dimensions;
    [SerializeField]
    private (Vector3 minBnd, Vector3 maxBnd) bounds;
    [SerializeField]
    private Vector3 damDimensions;
    [SerializeField]
    private Vector3 spawnPos;
    [SerializeField]
    private bool Velocity;
    [SerializeField]
    private bool Force;
    [SerializeField]
    private bool GravForce;
    [SerializeField]
    private bool PressureForce;
    [SerializeField]
    private bool ViscosityForce;
    [SerializeField]
    private bool tap;
    [SerializeField]
    private bool dam;
    [SerializeField]
    private bool particleDraw;
    Vector3 scale;
    [SerializeField]
    private Vector3 size;

    private float width;
    private float height;
    private float depth;
    Vector3 cellSize;
#pragma warning disable CS0108 // 'SPHRender.particleSystem' hides inherited member 'Component.particleSystem'. Use the new keyword if hiding was intended.
    private ParticleSystem particleSystem;
#pragma warning restore CS0108 // 'SPHRender.particleSystem' hides inherited member 'Component.particleSystem'. Use the new keyword if hiding was intended.

#pragma warning disable CS0414 // The field 'SPHRender.lastParticleIndex' is assigned but its value is never used
    int lastParticleIndex = 0;
#pragma warning restore CS0414 // The field 'SPHRender.lastParticleIndex' is assigned but its value is never used

    private OldSmoothedParticleHydrodynamics sph;
    private FastSmoothedParticleHydrodynamics sph2;
    private List<GameObject> renderedParticles;
    bool wait = false;
    // Start is called before the first frame update
    void Start()
    {
        bounds = (-Vector3.one, Vector3.one);
        bounds.minBnd.x *= dimensions.x / 2;
        bounds.minBnd.y *= dimensions.y / 2;
        bounds.minBnd.z *= dimensions.z / 2;
        bounds.minBnd += gridCenter;

        bounds.maxBnd.x *= dimensions.x / 2;
        bounds.maxBnd.y *= dimensions.y / 2;
        bounds.maxBnd.z *= dimensions.z / 2;
        bounds.maxBnd += gridCenter;

        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;

        cellSize = new Vector3(width / dimensions.x, height / dimensions.y, depth / dimensions.z);

        sph = GetComponent<OldSmoothedParticleHydrodynamics>();
        sph2 = GetComponent<FastSmoothedParticleHydrodynamics>();

        if (sph != null)
        {
            sph.initSPH((int)width, (int)height, (int)depth);
        }
        else if (sph2 != null)
        {
            sph2.initSPH((int)width, (int)height, (int)depth, bounds,cellSize);
        }

        renderedParticles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (dam)
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                if (sph != null)
                {
                    for (int x = 1; x < dimensions.x / (sph.SmoothingRadius * 2) - ((dimensions.x / (sph.SmoothingRadius * 2)) - damDimensions.x); x++)
                    {
                        for (int y = 1; y < dimensions.y / (sph.SmoothingRadius * 2) - ((dimensions.y / (sph.SmoothingRadius * 2)) - damDimensions.y); y++)
                        {
                            for (int z = 1; z < dimensions.z / (sph.SmoothingRadius * 2) - ((dimensions.z / (sph.SmoothingRadius * 2)) - damDimensions.z); z++)
                            {
                                sph.spawnParticle(new Vector3(x, y, z) + spawnPos);
                            }
                        }
                    }
                }
                else if (sph2 != null)
                {
                    for (int x = 1; x < dimensions.x / (sph2.SmoothingRadius * 2) - ((dimensions.x / (sph2.SmoothingRadius * 2)) - damDimensions.x); x++)
                    {
                        for (int y = 1; y < dimensions.y / (sph2.SmoothingRadius * 2) - ((dimensions.y / (sph2.SmoothingRadius * 2)) - damDimensions.y); y++)
                        {
                            for (int z = 1; z < dimensions.z / (sph2.SmoothingRadius * 2) - ((dimensions.z / (sph2.SmoothingRadius * 2)) - damDimensions.z); z++)
                            {                               
                                sph2.spawnParticle(new Vector3(x, y, z) + spawnPos);
                            }
                        }
                    }
                }
            }
        }
        if (tap)
        {
            if (Input.GetKey(KeyCode.F) && !wait)
            {
                StartCoroutine(Wait(.4f));
            }
        }
        if (particleDraw)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                    {
                        renderedParticles.Add(Instantiate(sphere, new Vector3(0, 0, 0), Quaternion.identity, parent));
                    }
                    renderedParticles[i].transform.localPosition = new Vector3(sph.Particles[i].Position.x, sph.Particles[i].Position.y, sph.Particles[i].Position.z );
                }
            }
            else
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                    {
                        renderedParticles.Add(Instantiate(sphere, new Vector3(0, 0, 0), Quaternion.identity, parent));
                    }
                    renderedParticles[i].transform.localPosition = new Vector3(sph2.Particles[i].position.x , sph2.Particles[i].position.y, sph2.Particles[i].position.z );
                }
            }
        }
    }

    IEnumerator Wait(float num)
    {
        wait = true;
        yield return new WaitForSeconds(num);
        if (sph != null)
        {
            sph.spawnParticle(spawnPos + new Vector3(UnityEngine.Random.value * 5, UnityEngine.Random.value * 5, UnityEngine.Random.value * 5));
        }
        else
        {
            sph2.spawnParticle(spawnPos + new Vector3(UnityEngine.Random.value * 5, UnityEngine.Random.value * 5, UnityEngine.Random.value * 5));
        }
        wait = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(gridCenter + size / 2, size);
        if (Velocity)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph.Particles[i].Velocity.normalized * Mathf.Clamp(sph.Particles[i].Velocity.normalized.magnitude, 0f, 1f));
                }
            }
            else if (sph2 != null)
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph2.Particles[i].velocity.normalized * Mathf.Clamp(sph2.Particles[i].velocity.normalized.magnitude, 0f, 1f));
                }
            }
        }
        if (Force)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.white;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph.Particles[i].Force.normalized * Mathf.Clamp(sph.Particles[i].Force.normalized.magnitude, 0f, 1f));
                }
            }
            else if (sph2 != null)
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.white;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph2.Particles[i].force.normalized * Mathf.Clamp(sph2.Particles[i].force.normalized.magnitude, 0f, 1f));
                }
            }
        }
        if (GravForce)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph.Particles[i].GravForce.normalized * Mathf.Clamp(sph.Particles[i].GravForce.normalized.magnitude, 0f, 1f));
                }
            }
            else if (sph2 != null)
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph2.Particles[i].gravForce.normalized * Mathf.Clamp(sph2.Particles[i].gravForce.normalized.magnitude, 0f, 1f));
                }
            }
        }
        if (PressureForce)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph.Particles[i].PressureForce.normalized * Mathf.Clamp(sph.Particles[i].PressureForce.normalized.magnitude, 0f, 1f));
                }
            }
            else if (sph2 != null)
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph2.Particles[i].pressureForce.normalized * Mathf.Clamp(sph2.Particles[i].pressureForce.normalized.magnitude, 0f, 1f));
                }
            }
        }
        if (ViscosityForce)
        {
            if (sph != null)
            {
                for (int i = 0; i < sph.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph.Particles[i].ViscosityForce.normalized * Mathf.Clamp(sph.Particles[i].ViscosityForce.normalized.magnitude, 0f, 1f));
                }
            }
            else if (sph2 != null)
            {
                for (int i = 0; i < sph2.Particles.Count; i++)
                {
                    if (i > renderedParticles.Count - 1)
                        continue;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(renderedParticles[i].transform.localPosition, sph2.Particles[i].viscosityForce.normalized * Mathf.Clamp(sph2.Particles[i].viscosityForce.normalized.magnitude, 0f, 1f));
                }
            }
        }
    }


}
