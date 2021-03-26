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
    private Vector3 size;
    [SerializeField]
    private Vector3 position;
    [SerializeField]
    private Vector3 resolution;
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
#pragma warning disable CS0108 // 'SPHRender.particleSystem' hides inherited member 'Component.particleSystem'. Use the new keyword if hiding was intended.
    private ParticleSystem particleSystem;
#pragma warning restore CS0108 // 'SPHRender.particleSystem' hides inherited member 'Component.particleSystem'. Use the new keyword if hiding was intended.

#pragma warning disable CS0414 // The field 'SPHRender.lastParticleIndex' is assigned but its value is never used
    int lastParticleIndex = 0;
#pragma warning restore CS0414 // The field 'SPHRender.lastParticleIndex' is assigned but its value is never used

    private SmoothedParticleHydrodynamics sph;
    private List<GameObject> renderedParticles;
    bool wait = false;
    // Start is called before the first frame update
    void Start()
    {
        sph = GetComponent<SmoothedParticleHydrodynamics>();
        sph.initSPH((int)resolution.x, (int)resolution.y, (int)resolution.z);
        renderedParticles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        scale = new Vector3((size.x / resolution.x), (size.y / resolution.y), (size.z / resolution.z));
        if (dam)
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                for (int x = 1; x < resolution.x/(sph.SmoothingRadius * 2)-((resolution.x / (sph.SmoothingRadius * 2))-damDimensions.x); x++)
                {
                    for (int y = 1; y < resolution.y / (sph.SmoothingRadius * 2) - ((resolution.y / (sph.SmoothingRadius * 2)) - damDimensions.y); y++)
                    {
                        for (int z = 1; z < resolution.z / (sph.SmoothingRadius * 2) - ((resolution.z / (sph.SmoothingRadius * 2)) - damDimensions.z); z++)
                        {
                            //sph.spawnParticle(new Vector3((x * sph.SmoothingRadius) + UnityEngine.Random.Range(0, 1f), (y * sph.SmoothingRadius), (z * sph.SmoothingRadius) + UnityEngine.Random.Range(0, 1f)));
                            //sph.spawnParticle(new Vector3((x * (sph.SmoothingRadius*1.5f)) + UnityEngine.Random.value, (y * (sph.SmoothingRadius * 1.5f)) + UnityEngine.Random.value, (z * (sph.SmoothingRadius * 1.5f)) + UnityEngine.Random.value)+spawnPos);
                            sph.spawnParticle(new Vector3(x,y,z)+spawnPos);
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
            for (int i = 0; i < sph.Particles.Count; i++)
            {
                if (i > renderedParticles.Count - 1)
                {
                    renderedParticles.Add(Instantiate(sphere, new Vector3(0, 0, 0), Quaternion.identity, parent));
                }
                renderedParticles[i].transform.localPosition = new Vector3(sph.Particles[i].Position.x * scale.x, sph.Particles[i].Position.y * scale.y, sph.Particles[i].Position.z * scale.z);
            }
        }
    }

    IEnumerator Wait(float num)
    {
        wait = true;
        yield return new WaitForSeconds(num);
        sph.spawnParticle(spawnPos + new Vector3(UnityEngine.Random.value * 5, UnityEngine.Random.value * 5, UnityEngine.Random.value * 5));
        wait = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(position + size / 2, size);
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
        }
    }


}
