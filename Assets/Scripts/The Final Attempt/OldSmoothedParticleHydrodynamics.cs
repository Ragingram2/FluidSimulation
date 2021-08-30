using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class OldSmoothedParticleHydrodynamics : MonoBehaviour
{
    [SerializeField]
    private ComputeShader shader;
    [SerializeField]
    private Vector3 G = new Vector3(0.0f, -9.8f, 0.0f);
    [SerializeField]
    private float smoothingRadius = 1.0f;
    [SerializeField]
    private float visc = 250.0f;
    [SerializeField]
    private float dt = 0.0008f;
    [SerializeField]
    private float restingDensity = 1000;
    [SerializeField]
    private float gasConstant = 2000;
    [SerializeField]
    private float buffer = 1;
    [SerializeField]
    private float boundDamping = -0.0001f;
    [SerializeField]
    private float Mass;
    [SerializeField]
    [Range(0f, 1f)]
    private float drag = 0.25f;
    [SerializeField]
    private bool steps;
    [SerializeField]
    private bool densities;
    [SerializeField]
    private bool GPU;

    private int neighborCount;

    private float gravityMultiplicator = 1.0f;

    private Vector3 size = new Vector3(0, 0, 0);

    public class Particle
    {
        private Vector3 position;
        private Vector3 velocity;
        private Vector3 force;
        private Vector3 gravForce;
        private Vector3 pressureForce;
        private Vector3 viscosityForce;
        private float density;
        private float pressure;

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        public Vector3 Force
        {
            get { return force; }
            set { force = value; }
        }
        public Vector3 GravForce
        {
            get { return gravForce; }
            set { gravForce = value; }
        }
        public Vector3 PressureForce
        {
            get { return pressureForce; }
            set { pressureForce = value; }
        }
        public Vector3 ViscosityForce
        {
            get { return viscosityForce; }
            set { viscosityForce = value; }
        }
        public float Density
        {
            get { return density; }
            set { density = value; }
        }
        public float Pressure
        {
            get { return pressure; }
            set { pressure = value; }
        }

#pragma warning disable CS0114 // 'SmoothedParticleHydrodynamics.Particle.ToString()' hides inherited member 'object.ToString()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
        public string ToString()
#pragma warning restore CS0114 // 'SmoothedParticleHydrodynamics.Particle.ToString()' hides inherited member 'object.ToString()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
        {
            return "Position: " + position +
                        "\n Velocity: " + velocity +
                        "\n Force: " + force +
                        "\n Density: " + density +
                        "\n Pressure: " + pressure;
        }
    }

    private List<Particle> particles = new List<Particle>();
    private List<Particle> allNeighbors = new List<Particle>();
    private List<int> neighborCounts = new List<int>();
    private List<List<float>> distances = new List<List<float>>();

    public void initSPH(int width, int height, int depth)
    {
        size = new Vector3(width, height, depth);
        particles = new List<Particle>();
    }

    public void spawnParticle(Vector3 position)
    {
        Particle p = new Particle();
        p.Position = position;
        particles.Add(p);
    }

    public List<Particle> CalculateNeighbors(Particle pi, int index)
    {
        List<Particle> neighbors = new List<Particle>();
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == pi)
                continue;
            float r = Vector3.Distance(pi.Position, particles[i].Position);
            if (r < 2 * smoothingRadius)
            {
                neighbors.Add(particles[i]);
                distances[index].Add(r);
            }
        }

        return neighbors;
    }

    void Step()
    {
        Profiler.BeginSample("Clear Lists");
        allNeighbors.Clear();
        neighborCounts.Clear();
        Profiler.EndSample();

        Profiler.BeginSample("Calculating all the neighbors");
        for (int i = 0; i < particles.Count; i++)
        {
            distances.Add(new List<float>());
            List<Particle> neighbors = CalculateNeighbors(particles[i], i);
            neighborCounts.Add(neighbors.Count);
            allNeighbors.AddRange(neighbors);
        }
        Profiler.EndSample();

        if (!GPU)
        {
            int start = 0;
            Profiler.BeginSample("NeighborRecalculation");
            for (int i = 0; i < particles.Count; i++)
            {
                for (int j = 0; j < neighborCounts[i]; j++)
                {
                    try
                    {
                        //Debug.Log("Distance: "+distances[i][j]);
                        particles[i].Density = Mathf.Max(ParticleDensity(particles[i], distances[i][j]), restingDensity);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Particle count: " + particles.Count);
                        Debug.Log("All Neighbors: " + allNeighbors.Count);
                        Debug.Log("Start Index: " + start);
                        Debug.Log("Index: " + j);
                        Debug.Log("End Index: " + (start + neighborCounts[i]));
                        throw e;
                    }
                }
                particles[i].Pressure = Mathf.Max(gasConstant * (particles[i].Density - restingDensity), 0);
            }
            Profiler.EndSample();

            Profiler.BeginSample("ForceCalculation");
            ForceCalculation();
            Profiler.EndSample();
        }
        else
        {
            if (particles.Count > 0)
            {

            }
        }
        Profiler.BeginSample("ParticleMovement");
        ParticleMovement();
        Profiler.EndSample();

    }

    private void ForceCalculation()
    {
        int start = 0;
        float distance = 0;
        Vector3 dif = Vector3.zero;
        Vector3 forcePressure = Vector3.zero;
        Vector3 forceViscosity = Vector3.zero;
        Vector3 forceGravity = Vector3.zero;
        int num = 0;

        Profiler.BeginSample("Looping through all particles");
        for (int i = 0; i < particles.Count; i++)
        {
            forcePressure = Vector3.zero;
            forceViscosity = Vector3.zero;

            if (i > 0)
            {
                start += neighborCounts[i - 1];
            }
            num = 0;
            Profiler.BeginSample("Looping through all of this particles Neighbors");
            for (int j = 0; j < neighborCounts[i]; j++)
            {
                num = start + j;
                Profiler.BeginSample("Direction, Particle, and Viscosity calculations");
                dif= particles[i].Position - allNeighbors[num].Position;
                distance = distances[i][j];

                forcePressure += ParticlePressure(particles[i], allNeighbors[num], dif.normalized, distance);

                forceViscosity += ParticleViscosity(particles[i], allNeighbors[num], distance);
                Profiler.EndSample();
            }
            Profiler.EndSample();

            forceGravity = G * particles[i].Density*smoothingRadius;

            Profiler.BeginSample("Setting particle properties");
            particles[i].GravForce = forceGravity;
            particles[i].PressureForce = forcePressure;
            particles[i].ViscosityForce = forceViscosity;
            particles[i].Force = forcePressure + forceViscosity + forceGravity;
            Profiler.EndSample();
        }
        Profiler.EndSample();
    }
    private void ParticleMovement()
    {
        foreach (Particle pi in particles)
        {
            if (pi.Density != 0)
            {
                pi.Velocity += dt * (pi.Force) / pi.Density;
            }
            pi.Velocity = new Vector3(pi.Velocity.x * (1f - drag), pi.Velocity.y * (1f - drag), pi.Velocity.z);
            pi.Position += dt * (pi.Velocity);
            applyBounds(pi);
        }
    }
    private float ParticleDensity(Particle _currentParticle, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return _currentParticle.Density += Mass * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(smoothingRadius, 9.0f))) * Mathf.Pow(smoothingRadius - _distance, 3.0f);
        }

        return _currentParticle.Density;
    }
    private Vector3 ParticlePressure(Particle _currentParticle, Particle _nextParticle, Vector3 _direction, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return -1 * _direction.normalized * Mass * (_currentParticle.Pressure + _nextParticle.Pressure) / (2.0f * _nextParticle.Density) *
                    (-45.0f / (Mathf.PI * Mathf.Pow(smoothingRadius, 6.0f))) * Mathf.Pow(smoothingRadius - _distance, 2.0f);
        }
        return Vector3.zero;
    }
    private Vector3 ParticleViscosity(Particle _currentParticle, Particle _nextParticle, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return visc * Mass * (_nextParticle.Velocity - _currentParticle.Velocity) / _nextParticle.Density * (45.0f / (Mathf.PI *
                    Mathf.Pow(smoothingRadius, 6.0f))) * (smoothingRadius - _distance);
        }

        return Vector3.zero;
    }
    void applyBounds(Particle p)
    {
        // enforce boundary conditions
        if (p.Position.x - buffer < 0.0f)
        {
            p.Velocity = new Vector3(p.Velocity.x * boundDamping, p.Velocity.y, p.Velocity.z);
            p.Position = new Vector3(buffer, p.Position.y, p.Position.z);
        }
        if (p.Position.x + buffer > size.x)
        {
            p.Velocity = new Vector3(p.Velocity.x * boundDamping, p.Velocity.y, p.Velocity.z);
            p.Position = new Vector3(size.x - buffer, p.Position.y, p.Position.z);
        }

        if (p.Position.y - buffer < 0.0f)
        {
            p.Velocity = new Vector3(p.Velocity.x, p.Velocity.y * boundDamping, p.Velocity.z);
            p.Position = new Vector3(p.Position.x, buffer, p.Position.z);
        }
        if (p.Position.y + buffer > size.y)
        {
            p.Velocity = new Vector3(p.Velocity.x, p.Velocity.y * boundDamping, p.Velocity.z);
            p.Position = new Vector3(p.Position.x, size.y - buffer, p.Position.z);
        }

        if (p.Position.z - buffer < 0.0f)
        {
            p.Velocity = new Vector3(p.Velocity.x, p.Velocity.y, p.Velocity.z * boundDamping);
            p.Position = new Vector3(p.Position.x, p.Position.y, buffer);
        }
        if (p.Position.z + buffer > size.z)
        {
            p.Velocity = new Vector3(p.Velocity.x, p.Velocity.y, p.Velocity.z * boundDamping);
            p.Position = new Vector3(p.Position.x, p.Position.y, size.z - buffer);
        }
    }

    private void Update()
    {
        Profiler.BeginSample("Checking for Step");
        if (steps)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                Step();
            }
        }
        else
        {
            Profiler.BeginSample("Step");
            Step();
            Profiler.EndSample();
        }
        Profiler.EndSample();
    }

    private void OnDrawGizmos()
    {
        if (densities)
        {
            foreach (Particle pi in particles)
            {
                DrawDensities(new Vector3(5, 5, 5), size, pi);
            }
        }
    }


    public void DrawDensities(Vector3 dimensions, Vector3 resolution, Particle p)
    {
        Vector3 Scale = new Vector3(dimensions.x / resolution.x, dimensions.y / resolution.y, dimensions.z / resolution.z);
        Vector3 actualPos = new Vector3(p.Position.x * (Scale.x), p.Position.y * (Scale.y), p.Position.z * (Scale.z));
        //GizmoUtils.DrawText(GUI.skin, String.Format("{0:0.0},{1:0.0},{2:0.0}", p.Velocity.x,p.Velocity.y,p.Velocity.z), actualPos, Color.blue, 20, 0.5f);
        GizmoUtils.DrawText(GUI.skin, String.Format("{0:0.0}", p.Density), actualPos, Color.blue, 20, 0.5f);
    }


    public List<Particle> Particles
    {
        get
        {
            return new List<Particle>(particles);
        }
    }
    public float SmoothingRadius
    {
        get { return smoothingRadius; }
        set { smoothingRadius = value; }
    }

}
