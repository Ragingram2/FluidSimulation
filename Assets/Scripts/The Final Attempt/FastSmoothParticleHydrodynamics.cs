using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class FastSmoothedParticleHydrodynamics : MonoBehaviour
{
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
    private (Vector3 minBnd, Vector3 maxBnd) bounds;
    [SerializeField]
    private Vector3 gridCenter = Vector3.zero;
    [SerializeField]
    private Vector3 dimensions;
    [SerializeField]
    private Vector3 searchSize;
    [SerializeField]
    private Material mat;
    [SerializeField]
    private int particleCount = 25;
    Vector3 cellSize;
    private float width;
    private float height;
    private float depth;
    private Dictionary<string, HashSet<Particle>> cells = new Dictionary<string, HashSet<Particle>>();
    private HashSet<Particle> nearby;

    private float gravityMultiplicator = 20.0f;

    private Vector3 size = new Vector3(0, 0, 0);

    public class Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public Vector3 gravForce;
        public Vector3 pressureForce;
        public Vector3 viscosityForce;
        public Vector3 size;
        public Vector3Int[] indicies;
        public string key;
        public float density;
        public float pressure;
        public GameObject gameObject;

        public void MoveClient(float dt, float drag, float buffer, float boundDamping)
        {
            if (density != 0)
            {
                velocity += dt * (force) / density;
            }
            velocity = new Vector3(velocity.x * (1f - drag), velocity.y * (1f - drag), velocity.z);
            position += dt * (velocity);
            applyBounds(buffer, boundDamping);
            position += velocity * Time.deltaTime;
            gameObject.transform.position = position;
        }

        void applyBounds(float buffer, float boundDamping)
        {
            // enforce boundary conditions
            if (position.x - buffer < 0.0f)
            {
                velocity = new Vector3(velocity.x * boundDamping, velocity.y, velocity.z);
                position = new Vector3(buffer, position.y, position.z);
            }
            if (position.x + buffer > size.x)
            {
                velocity = new Vector3(velocity.x * boundDamping, velocity.y, velocity.z);
                position = new Vector3(size.x - buffer, position.y, position.z);
            }

            if (position.y - buffer < 0.0f)
            {
                velocity = new Vector3(velocity.x, velocity.y * boundDamping, velocity.z);
                position = new Vector3(position.x, buffer, position.z);
            }
            if (position.y + buffer > size.y)
            {
                velocity = new Vector3(velocity.x, velocity.y * boundDamping, velocity.z);
                position = new Vector3(position.x, size.y - buffer, position.z);
            }

            if (position.z - buffer < 0.0f)
            {
                velocity = new Vector3(velocity.x, velocity.y, velocity.z * boundDamping);
                position = new Vector3(position.x, position.y, buffer);
            }
            if (position.z + buffer > size.z)
            {
                velocity = new Vector3(velocity.x, velocity.y, velocity.z * boundDamping);
                position = new Vector3(position.x, position.y, size.z - buffer);
            }
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

    public void initSPH(int width, int height, int depth, (Vector3 minBnd, Vector3 maxBnd) _bounds, Vector3 _cellSize)
    {
        cellSize = _cellSize;
        bounds = _bounds;
        size = new Vector3(width, height, depth);
        particles = new List<Particle>();
    }

    public void spawnParticle(Vector3 position)
    {
        Particle p = new Particle();
        p.position = position;
        p.indicies = null;
        p.key = "0.0.0";
        particles.Add(p);
        insertClient(p);
    }

    void Step()
    {
        if (nearby != null)
        {
            nearby.Clear();
        }

        Profiler.BeginSample("Density and Pressure calculation");
        for (int i = 0; i < particles.Count; i++)
        {
            UpdateClient(particles[i]);
            nearby = FindNearby(particles[i].position, searchSize);
            foreach (Particle neighbor in nearby)
            {
                particles[i].density = Mathf.Max(ParticleDensity(particles[i], Vector3.Distance(particles[i].position, neighbor.position)), restingDensity);
            }
            particles[i].pressure = Mathf.Max(gasConstant * (particles[i].density - restingDensity), 0);
        }
        Profiler.EndSample();

        Profiler.BeginSample("Force calculation");
        for (int i = 0; i < particles.Count; i++)
        {
            ForceCalculation(particles[i]);
        }
        Profiler.EndSample();

        Profiler.BeginSample("Particle Movement");
        ParticleMovement();
        Profiler.EndSample();

    }

    private void ForceCalculation(Particle p)
    {
        float distance = 0;
        Vector3 dif = Vector3.zero;
        Vector3 forcePressure = Vector3.zero;
        Vector3 forceViscosity = Vector3.zero;
        Vector3 forceGravity = Vector3.zero;

        Profiler.BeginSample("Force Calculation - Recalculating neighbors");
        nearby = FindNearby(p.position, searchSize);
        Profiler.EndSample();

        Profiler.BeginSample("Force Calculation - Looping through all of this particles Neighbors");
        foreach (Particle neighbor in nearby)
        {
            Profiler.BeginSample("Direction, Particle, and Viscosity calculations");
            dif = p.position - neighbor.position;
            distance = Vector3.Distance(p.position, neighbor.position);

            forcePressure += ParticlePressure(p, neighbor, dif.normalized, distance);

            forceViscosity += ParticleViscosity(p, neighbor, distance);
            Profiler.EndSample();
        }
        Profiler.EndSample();

        forceGravity = G * p.density * gravityMultiplicator;

        Profiler.BeginSample("Setting particle properties");
        p.gravForce = forceGravity;
        p.pressureForce = forcePressure;
        p.viscosityForce = forceViscosity;
        p.force = forcePressure + forceViscosity + forceGravity;
        Profiler.EndSample();
    }
    private void ParticleMovement()
    {
        foreach (Particle pi in particles)
        {
            pi.MoveClient(dt, drag, buffer, boundDamping);
        }
    }
    private float ParticleDensity(Particle _currentParticle, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return _currentParticle.density += Mass * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(smoothingRadius, 9.0f))) * Mathf.Pow(smoothingRadius - _distance, 3.0f);
        }

        return _currentParticle.density;
    }
    private Vector3 ParticlePressure(Particle _currentParticle, Particle _nextParticle, Vector3 _direction, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return -1 * _direction.normalized * Mass * (_currentParticle.pressure + _nextParticle.pressure) / (2.0f * _nextParticle.density) *
                    (-45.0f / (Mathf.PI * Mathf.Pow(smoothingRadius, 6.0f))) * Mathf.Pow(smoothingRadius - _distance, 2.0f);
        }
        return Vector3.zero;
    }
    private Vector3 ParticleViscosity(Particle _currentParticle, Particle _nextParticle, float _distance)
    {
        if (_distance < smoothingRadius)
        {
            return visc * Mass * (_nextParticle.velocity - _currentParticle.velocity) / _nextParticle.density * (45.0f / (Mathf.PI *
                    Mathf.Pow(smoothingRadius, 6.0f))) * (smoothingRadius - _distance);
        }

        return Vector3.zero;
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
        Vector3 actualPos = new Vector3(p.position.x * (Scale.x), p.position.y * (Scale.y), p.position.z * (Scale.z));
        //GizmoUtils.DrawText(GUI.skin, String.Format("{0:0.0},{1:0.0},{2:0.0}", p.Velocity.x,p.Velocity.y,p.Velocity.z), actualPos, Color.blue, 20, 0.5f);
        GizmoUtils.DrawText(GUI.skin, String.Format("{0:0.0}", p.density), actualPos, Color.blue, 20, 0.5f);
    }

    public void UpdateClient(Particle _client)
    {
        removeClient(_client);
        insertClient(_client);
    }

    private void insertClient(Particle _client)
    {
        float xPos, yPos, zPos, width, height, depth;
        xPos = _client.position.x;
        yPos = _client.position.y;
        zPos = _client.position.z;

        width = _client.size.x;
        height = _client.size.y;
        depth = _client.size.z;

        var i1 = getCellIndex(xPos - width / 2f, yPos - height / 2f, zPos - depth / 2f);
        var i2 = getCellIndex(xPos + width / 2f, yPos + height / 2f, zPos + depth / 2f);

        _client.indicies = new Vector3Int[2] { i1, i2 };

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);
                    if (!cells.ContainsKey(key))
                    {
                        cells[key] = new HashSet<Particle>();
                    }
                    _client.key = key;
                    cells[key].Add(_client);
                }
            }
        }
    }
    private void removeClient(Particle _client)
    {
        Vector3Int i1, i2;
        i1 = _client.indicies[0];
        i2 = _client.indicies[1];

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);
                    cells[key].Remove(_client);
                }
            }
        }
    }
    public HashSet<Particle> FindNearby(Vector3 _position, Vector3 _searchArea)
    {
        float xPos, yPos, zPos, width, height, depth;
        xPos = _position.x;
        yPos = _position.y;
        zPos = _position.z;
        width = _searchArea.x;
        height = _searchArea.y;
        depth = _searchArea.z;

        var i1 = getCellIndex(xPos - width / 2f, yPos - height / 2f, zPos - depth / 2f);
        var i2 = getCellIndex(xPos + width / 2f, yPos + height / 2f, zPos + depth / 2f);

        var clients = new HashSet<Particle>();

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);

                    if (cells.ContainsKey(key))
                    {
                        foreach (var value in cells[key])
                        {
                            clients.Add(value);
                        }
                    }
                }
            }
        }
        return clients;
    }

    private string generateKey(int _x, int _y, int _z)
    {
        string key = _x + "." + _y + "." + _z;
        return key;
    }

    private Vector3Int getCellIndex(float _x, float _y, float _z)
    {
        float xVal = Mathf.Clamp01((_x - bounds.minBnd.x) / (width));
        float yVal = Mathf.Clamp01((_y - bounds.minBnd.y) / (height));
        float zVal = Mathf.Clamp01((_z - bounds.minBnd.z) / (depth));

        int xIndex = (int)Mathf.Floor(xVal * (dimensions.x - 1));
        int yIndex = (int)Mathf.Floor(yVal * (dimensions.y - 1));
        int zIndex = (int)Mathf.Floor(zVal * (dimensions.z - 1));
        return new Vector3Int(xIndex, yIndex, zIndex);
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

