using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR;
using Unity.Jobs;
using UnityEngine.Rendering;
using System;
using UnityEngine.Assertions.Must;

public class FluidSimulation : MonoBehaviour
{
    [SerializeField]
    [Range(0, 1)]
    private float viscosity = 0;

    [SerializeField]
    [Range(0, 20)]
    private float diffuseRate = 0;
    [SerializeField]
    [Range(0, 5f)]
    private float timeStep = 0;

    [SerializeField]
    private bool displayDensities = true;

    [SerializeField]
    private bool displayParticles = true;

    [SerializeField]
    private bool displayVelocites = true;

    //[SerializeField]
    //private bool gpuCompute = true;

    [SerializeField]
    private float density = 10;

    [SerializeField]
    private Vector3 force = new Vector3(0, 0, 0);

    [SerializeField]
    private Vector3 dyeStart;

#pragma warning disable CS0414 // The field 'FluidSimulation.totalDye' is assigned but its value is never used
    private float totalDye;
#pragma warning restore CS0414 // The field 'FluidSimulation.totalDye' is assigned but its value is never used

    [SerializeField]
    private ComputeShader velocityStep;

    [SerializeField]
    private ComputeShader densityStep;

    private ComputeBuffer buffer;

    private FluidParticle[] particles;

    private Vector3[] prevVelocityField;
    private Vector3[] velocityField;
    private float[] prevDensityField;
    private float[] densityField;

#pragma warning disable CS0414 // The field 'FluidSimulation.thing' is assigned but its value is never used
    bool thing = false;
#pragma warning restore CS0414 // The field 'FluidSimulation.thing' is assigned but its value is never used

    private FluidFieldRender fieldRender;
    private int densityKernel;
    private int velocityKernel;
    private float size;

    // Start is called before the first frame update
    private void Start()
    {
        //gets the component that handles rendering of the particles
        fieldRender = GetComponent<FluidFieldRender>();
        fieldRender.Initialze();

        size = fieldRender.resolution.x;
        //list of all particles;
        particles = fieldRender.Particles;

        //initialization of the previous and current velocty fields
        prevVelocityField = new Vector3[(int)particles.Length];
        velocityField = new Vector3[(int)particles.Length];

        //initialization of the previous and current density fields
        prevDensityField = new float[(int)particles.Length];
        densityField = new float[(int)particles.Length];

        for (int i = 0; i < particles.Length; i++)
        {
            fieldRender.Particles[i].Initialize(timeStep, diffuseRate, viscosity);
        }
        velocityKernel = velocityStep.FindKernel("Velocity");
        densityKernel = densityStep.FindKernel("Density");

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        fieldRender.DisplayDensities = displayDensities;
        fieldRender.DisplayParticles = displayParticles;

        #region NO
        //if (false)
        //{
        //    for (int i = 0; i < particles.Length; i++)
        //    {
        //        prevVelocityField[i] = particles[i].prevVelocity;
        //        prevDensityField[i] = particles[i].prevDensity;
        //        velocityField[i] = particles[i].velocity;
        //        densityField[i] = particles[i].density;
        //    }

        //    List<float> u = new List<float>();
        //    List<float> v = new List<float>();
        //    List<float> p = new List<float>();
        //    velocityField.ToList().ForEach(x => u.Add(x.x));
        //    velocityField.ToList().ForEach(x => v.Add(x.y));
        //    velocityField.ToList().ForEach(x => p.Add(x.z));

        //    List<float> u0 = new List<float>();
        //    List<float> v0 = new List<float>();
        //    List<float> p0 = new List<float>();

        //    prevVelocityField.ToList().ForEach(x => u0.Add(x.x));
        //    prevVelocityField.ToList().ForEach(x => v0.Add(x.y));
        //    prevVelocityField.ToList().ForEach(x => p0.Add(x.z));

        //    //if (!thing)
        //    //{
        //    //    densityField[IX(2, 2, 2)] = 100f;
        //    //    prevDensityField[IX(2, 2, 2)] = 100f;
        //    //    thing = true;
        //    //}

        //    if (Input.GetKeyUp(KeyCode.Space))
        //    {
        //        fluidStep((int)fieldRender.Resolution.x - 1, densityField, prevDensityField, u.ToArray(), v.ToArray(), p.ToArray(), u0.ToArray(), v0.ToArray(), p0.ToArray(), .0001f, Time.deltaTime);
        //    }

        //    for (int i = 0; i < particles.Length; i++)
        //    {
        //        particles[i].UpdateParticle();//the previous velocities and densities for the next update step are calculated here
        //        //particles[i].SetVelocity(velocityField[i]);
        //        //particles[i].SetDensity(densityField[i]);
        //        particles[i].SetVelocity(velocityField[i]);
        //        particles[i].SetDensity(densityField[i]);

        //    }
        //}
        #endregion

        if (Input.GetKey(KeyCode.LeftControl))
        {
            for (int i = 0; i < fieldRender.Particles.Length; i++)
            {
                totalDye = 0;
                fieldRender.Particles[i].SetDensity(0);
                fieldRender.Particles[i].SetPrevDensity(0);
                fieldRender.Particles[i].SetVelocity(Vector3.zero);
                fieldRender.Particles[i].SetPrevVelocity(Vector3.zero);
            }

        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            fieldRender.Particles[IX(dyeStart.x, dyeStart.y, dyeStart.z)].AddPrevVelocity(force);
            fieldRender.Particles[IX(dyeStart.x, dyeStart.y, dyeStart.z)].AddPrevDensity(density);
        
        }

        ParallelFluidStep();


        for (int i = 0; i < fieldRender.Particles.Length; i++)
        {
            fieldRender.Particles[i].UpdateParticle(timeStep, diffuseRate, viscosity);
        }

    }

    public void ParallelFluidStep()
    {
        buffer = new ComputeBuffer(fieldRender.Particles.Length, sizeof(float) * 14, ComputeBufferType.Structured);

        float sizeX = fieldRender.resolution.x;
        float sizeY = fieldRender.resolution.y;
        float sizeZ = fieldRender.resolution.z;
        ////Velocity
        buffer.SetData(fieldRender.Particles);
        velocityStep.SetBuffer(velocityKernel, "particles", buffer);
        velocityStep.SetInt("sizeX", (int)sizeX);
        velocityStep.SetInt("sizeY", (int)sizeY);
        velocityStep.SetInt("sizeZ", (int)sizeZ);
        velocityStep.Dispatch(velocityKernel, (int)(sizeX * sizeY * sizeZ), 1, 1);
        buffer.GetData(fieldRender.Particles);

        //Debug.Log(fieldRender.Particles[5].GetPrevVelocityX()+ fieldRender.Particles[5].GetPrevVelocityY()+ fieldRender.Particles[5].GetPrevVelocityZ());
        //Debug.Log(fieldRender.Particles[75].Print());

        //Density
        buffer.SetData(fieldRender.Particles);
        densityStep.SetBuffer(densityKernel, "particles", buffer);
        densityStep.SetInt("sizeX", (int)sizeX);
        densityStep.SetInt("sizeY", (int)sizeY);
        densityStep.SetInt("sizeZ", (int)sizeZ);
        densityStep.Dispatch(densityKernel, (int)(sizeX * sizeY * sizeZ), 1, 1);
        buffer.GetData(fieldRender.Particles);


        //Debug.Log(fieldRender.Particles[15].GetVelocityY());

        //if (sizeZ > 2)
        //{
        //    #region densityBounds;
        //    fieldRender.Particles[IX(0, 0, 0)].SetDensity(0.33f * (fieldRender.Particles[IX(1, 0, 0)].GetDensity() + fieldRender.Particles[IX(0, 1, 0)].GetDensity() + fieldRender.Particles[IX(0, 0, 1)].GetDensity()));
        //    fieldRender.Particles[IX(0, 0, sizeZ - 1)].SetDensity(0.33f * (fieldRender.Particles[IX(1, 0, sizeZ - 1)].GetDensity() + fieldRender.Particles[IX(0, 1, sizeZ - 1)].GetDensity() + fieldRender.Particles[IX(0, 0, sizeZ - 2)].GetDensity()));
        //    fieldRender.Particles[IX(0, sizeY - 1, 0)].SetDensity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, 0)].GetDensity() + fieldRender.Particles[IX(0, sizeY - 2, 0)].GetDensity() + fieldRender.Particles[IX(0, sizeY - 1, 1)].GetDensity()));
        //    fieldRender.Particles[IX(sizeX - 1, 0, 0)].SetDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, 0)].GetDensity() + fieldRender.Particles[IX(sizeX - 1, 1, 0)].GetDensity() + fieldRender.Particles[IX(sizeX- 1, 0, 1)].GetDensity()));

        //    fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 1)].SetDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, sizeZ - 1)].GetDensity() + particles[IX(sizeX - 1, sizeY - 2, sizeZ - 1)].GetDensity() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 2)].GetDensity()));
        //    fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 0)].SetDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, 0)].GetDensity() + particles[IX(sizeX - 1, sizeY - 2, 0)].GetDensity() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 1)].GetDensity()));
        //    fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 1)].SetDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, sizeZ - 1)].GetDensity() + particles[IX(sizeX - 1, 1, sizeZ - 1)].GetDensity() + fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 2)].GetDensity()));
        //    fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 1)].SetDensity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, sizeZ - 1)].GetDensity() + particles[IX(0, sizeY - 2, sizeZ - 1)].GetDensity() + fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 2)].GetDensity()));
        //    #endregion

        //    #region previous densityBounds;
        //    fieldRender.Particles[IX(0, 0, 0)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(1, 0, 0)].GetPrevDensity() + fieldRender.Particles[IX(0, 1, 0)].GetPrevDensity() + fieldRender.Particles[IX(0, 0, 1)].GetPrevDensity()));
        //    fieldRender.Particles[IX(0, 0, sizeZ - 1)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(1, 0, sizeZ - 1)].GetPrevDensity() + fieldRender.Particles[IX(0, 1, sizeZ - 1)].GetPrevDensity() + fieldRender.Particles[IX(0, 0, sizeZ - 2)].GetPrevDensity()));
        //    fieldRender.Particles[IX(0, sizeY - 1, 0)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, 0)].GetPrevDensity() + fieldRender.Particles[IX(0, sizeY - 2, 0)].GetPrevDensity() + fieldRender.Particles[IX(0, sizeY - 1, 1)].GetPrevDensity()));
        //    fieldRender.Particles[IX(sizeX - 1, 0, 0)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, 0)].GetPrevDensity() + fieldRender.Particles[IX(sizeX - 1, 1, 0)].GetPrevDensity() + fieldRender.Particles[IX(sizeX - 1, 0, 1)].GetPrevDensity()));

        //    fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 1)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, sizeZ - 1)].GetPrevDensity() + particles[IX(sizeX - 1, sizeY - 2, sizeZ - 2)].GetPrevDensity() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 2)].GetPrevDensity()));
        //    fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 1)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, sizeZ - 1)].GetPrevDensity() + particles[IX(sizeX - 1, 1, sizeZ - 1)].GetPrevDensity() + fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 2)].GetPrevDensity()));
        //    fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 1)].SetPrevDensity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, sizeZ - 1)].GetPrevDensity() + particles[IX(0, sizeY - 2, sizeZ - 1)].GetPrevDensity() + fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 2)].GetPrevDensity()));
        //    #endregion

        #region VelocityBounds
        fieldRender.Particles[IX(0, 0, 0)].SetVelocity(0.33f * (fieldRender.Particles[IX(1, 0, 0)].GetVelocityX() + fieldRender.Particles[IX(0, 1, 0)].GetVelocityY() + fieldRender.Particles[IX(0, 0, 1)].GetVelocityZ()));
        fieldRender.Particles[IX(0, 0, sizeZ - 1)].SetVelocity(0.33f * (fieldRender.Particles[IX(1, 0, sizeZ - 1)].GetVelocityX() + fieldRender.Particles[IX(0, 1, sizeZ - 1)].GetVelocityY() + fieldRender.Particles[IX(0, 0, sizeZ - 2)].GetVelocityZ()));
        fieldRender.Particles[IX(0, sizeY - 1, 0)].SetVelocity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, 0)].GetVelocityX() + fieldRender.Particles[IX(0, sizeY - 2, 0)].GetVelocityY() + fieldRender.Particles[IX(0, sizeY - 1, 1)].GetVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, 0, 0)].SetVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, 0)].GetVelocityX() + fieldRender.Particles[IX(sizeX - 1, 1, 0)].GetVelocityY() + fieldRender.Particles[IX(sizeX - 1, 0, 1)].GetVelocityZ()));

        fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 1)].SetVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, sizeZ - 1)].GetVelocityX() + particles[IX(sizeX - 1, sizeY - 2, sizeZ - 2)].GetVelocityY() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 2)].GetVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 0)].SetVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, 0)].GetVelocityX() + particles[IX(sizeX - 1, sizeY - 2, 0)].GetVelocityY() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 1)].GetVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 1)].SetVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, sizeZ - 1)].GetVelocityX() + particles[IX(sizeX - 1, 1, sizeZ - 1)].GetVelocityY() + fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 2)].GetVelocityZ()));
        fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 1)].SetVelocity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, sizeZ - 1)].GetVelocityX() + particles[IX(0, sizeY - 2, sizeZ - 1)].GetVelocityY() + fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 2)].GetVelocityZ()));
        #endregion

        #region previous VelocityBounds
        fieldRender.Particles[IX(0, 0, 0)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(1, 0, 0)].GetPrevVelocityX() + fieldRender.Particles[IX(0, 1, 0)].GetPrevVelocityY() + fieldRender.Particles[IX(0, 0, 1)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(0, 0, sizeZ - 1)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(1, 0, sizeZ - 1)].GetPrevVelocityX() + fieldRender.Particles[IX(0, 1, sizeZ - 1)].GetPrevVelocityY() + fieldRender.Particles[IX(0, 0, sizeZ - 2)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(0, sizeY - 1, 0)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, 0)].GetPrevVelocityX() + fieldRender.Particles[IX(0, sizeY - 2, 0)].GetPrevVelocityY() + fieldRender.Particles[IX(0, sizeY - 1, 1)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, 0, 0)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, 0)].GetPrevVelocityX() + fieldRender.Particles[IX(sizeX - 1, 1, 0)].GetPrevVelocityY() + fieldRender.Particles[IX(sizeX - 1, 0, 1)].GetPrevVelocityZ()));

        fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 1)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, sizeZ - 1)].GetPrevVelocityX() + particles[IX(sizeX - 1, sizeY - 2, sizeZ - 2)].GetPrevVelocityY() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, sizeZ - 2)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 0)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, sizeY - 1, 0)].GetPrevVelocityX() + particles[IX(sizeX - 1, sizeY - 2, 0)].GetPrevVelocityY() + fieldRender.Particles[IX(sizeX - 1, sizeY - 1, 1)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 1)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(sizeX - 2, 0, sizeZ - 1)].GetPrevVelocityX() + particles[IX(sizeX - 1, 1, sizeZ - 1)].GetPrevVelocityY() + fieldRender.Particles[IX(sizeX - 1, 0, sizeZ - 2)].GetPrevVelocityZ()));
        fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 1)].SetPrevVelocity(0.33f * (fieldRender.Particles[IX(1, sizeY - 1, sizeZ - 1)].GetPrevVelocityX() + particles[IX(0, sizeY - 2, sizeZ - 1)].GetPrevVelocityY() + fieldRender.Particles[IX(0, sizeY - 1, sizeZ - 2)].GetPrevVelocityZ()));
        #endregion
        //}

        buffer.Dispose();
    }



    void fluidStep(int N, float[] cells, float[] previousCells, float[] xVel, float[] yVel, float[] zVel, float[] xVel0, float[] yVel0, float[] zVel0, float visc, float dt)
    {
        //Add Sources
        //addSource(N, u, u0, dt);
        //addSource(N, v, v0, dt);

        //swap and diffuse
        //SWAP(u0, u);
        diffuse(N, 1, xVel0, xVel, visc, dt);

        //SWAP(v0, v);
        diffuse(N, 2, yVel0, yVel, visc, dt);

        //SWAP(p0, p);
        diffuse(N, 3, zVel0, zVel, visc, dt);

        //project
        project(N, xVel0, yVel0, zVel0, xVel, yVel);

        //swap again
        //SWAP(u0, u);
        //SWAP(v0, v);
        //SWAP(p0, p);

        //advect
        advect(N, 1, xVel, xVel0, xVel0, yVel0, zVel0, dt);
        advect(N, 2, yVel, yVel0, xVel0, yVel0, zVel0, dt);
        advect(N, 3, zVel, zVel0, xVel0, yVel0, zVel0, dt);

        //project one last time
        project(N, xVel, yVel, zVel, xVel0, yVel0);

        diffuse(N, 0, previousCells, cells, 1f, dt);
        advect(N, 0, cells, previousCells, xVel, yVel, zVel, dt);

    }
    public void addSource(int N, float[] cells, float[] sources, float dt)
    {
        int size = (N) * (N) * (N);
        for (int i = 0; i < size; i++)
        {
            cells[i] += dt * sources[i];
        }
    }
    public void advect(int N, int b, float[] cells, float[] previousCells, float[] xVel, float[] yVel, float[] zVel, float dt)
    {
        int i, j, k, i0, j0, k0, i1, j1, k1;
        float xComp, yComp, zComp, s0, t0, s1, t1, dt0;
        dt0 = dt * N;
        for (i = 1; i <= N; i++)
        {
            for (j = 1; j < N; j++)
            {
                for (k = 1; k < N; k++)
                {
                    xComp = i - dt0 * xVel[IX(i, j, k)];
                    yComp = j - dt0 * yVel[IX(i, j, k)];
                    zComp = k - dt0 * zVel[IX(i, j, k)];
                    if (xComp < 0.5)
                        xComp = 0.5f;
                    if (xComp > N + 0.5)
                        xComp = N + 0.5f;
                    i0 = (int)xComp; i1 = i0 + 1;

                    if (yComp < 0.5)
                        yComp = 0.5f;
                    if (yComp > N + 0.5)
                        yComp = N + 0.5f;
                    j0 = (int)yComp; j1 = j0 + 1;

                    if (zComp < 0.5)
                        zComp = 0.5f;
                    if (zComp > N + 0.5)
                        zComp = N + 0.5f;
                    k0 = (int)zComp;
                    k1 = k0 + 1;

                    s1 = xComp - i0; s0 = 1 - s1; t1 = yComp - j0; t0 = 1 - t1;
                    cells[IX(i, j, k)] = s0 * (t0 * previousCells[IX(i0, j0, k0)] + t1 * previousCells[IX(i0, j1, k0)]) + s1 * (t0 * previousCells[IX(i1, j0, k0)] + t1 * previousCells[IX(i1, j1, k0)]);//might need to add a third thing here
                }
            }
        }
        set_bnd(N, b, cells);
    }
    public void diffuse(int N, int b, float[] cells, float[] previousCells, float diff, float dt)
    {
        int i, j, k, w;
        float a = dt * diff * N * N * N;
        for (k = 0; k < 20; k++)
        {
            for (i = 1; i < N; i++)
            {
                for (j = 1; j < N; j++)
                {
                    for (w = 1; w < N; w++)
                    {
                        //cells[IX(i, j, w)] = (previousCells[IX(i, j, w)] + a * (particles[IX(i, j, w)].Neighbors.Sum(x => x.Density))) / (1 + (particles[IX(i, j, w)].Neighbors.Count * a));
                        cells[IX(i, j, w)] = (previousCells[IX(i, j, w)] + a * (cells[IX(i - 1, j, w)] + cells[IX(i + 1, j, w)] + cells[IX(i, j - 1, w)] + cells[IX(i, j + 1, w)] + cells[IX(i, j, w - 1)] + cells[IX(i, j, w + 1)])) / (1 + 6 * a);
                    }
                }
            }
            set_bnd(N, b, cells);
        }

    }
    public void project(int N, float[] xVel, float[] yVel, float[] zVel, float[] gradient, float[] div)
    {
        int i, j, k, w;
        float h;
        h = 1.0f / N;
        for (i = 1; i < N; i++)
        {
            for (j = 1; j < N; j++)
            {
                for (w = 1; w < N; w++)
                {

                    try
                    {
                        div[IX(i, j, w)] = -0.5f * h * (xVel[IX(i + 1, j, w)] - xVel[IX(i - 1, j, w)] + yVel[IX(i, j + 1, w)] - yVel[IX(i, j - 1, w)] + zVel[IX(i, j, w + 1)] - zVel[IX(i, j, w - 1)]);
                        gradient[IX(i, j, w)] = 0;
                    }
                    catch (Exception e)
                    {
                        Debug.Log(IX(i, j, w));
                        Debug.Log("i:" + i);
                        Debug.Log("j:" + j);
                        Debug.Log("w:" + w);
                        throw e;
                    }
                }
            }
        }
        set_bnd(N, 0, div); set_bnd(N, 0, gradient);
        for (k = 0; k < 20; k++)
        {
            for (i = 1; i < N; i++)
            {
                for (j = 1; j < N; j++)
                {
                    for (w = 1; w < N; w++)
                    {
                        gradient[IX(i, j, w)] = (div[IX(i, j, w)] + gradient[IX(i - 1, j, w)] + gradient[IX(i + 1, j, w)] + gradient[IX(i, j - 1, w)] + gradient[IX(i, j + 1, w)] + gradient[IX(i, j, w + 1)] + gradient[IX(i, j, w - 1)]) / 6;
                    }
                }
            }
            set_bnd(N, 0, gradient);
        }
        for (i = 1; i < N; i++)
        {
            for (j = 1; j < N; j++)
            {
                for (w = 1; w < N; w++)
                {
                    xVel[IX(i, j, w)] -= 0.5f * (gradient[IX(i + 1, j, w)] - gradient[IX(i - 1, j, w)]) / h;
                    yVel[IX(i, j, w)] -= 0.5f * (gradient[IX(i, j + 1, w)] - gradient[IX(i, j - 1, w)]) / h;
                    zVel[IX(i, j, w)] -= 0.5f * (gradient[IX(i, j, w + 1)] - gradient[IX(i, j, w - 1)]) / h;
                }
            }
        }
        set_bnd(N, 1, xVel); set_bnd(N, 2, yVel); set_bnd(N, 3, zVel);
    }
    void set_bnd(int N, int b, float[] cells)
    {
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //Left and Right bounds
                cells[IX(0, i, j)] = b == 1 ? -cells[IX(1, i, j)] : cells[IX(1, i, j)];
                cells[IX(N, i, j)] = b == 1 ? -cells[IX(N - 1, i, j)] : cells[IX(N - 1, i, j)];

                //Upper and Lower bounds
                cells[IX(i, 0, j)] = b == 2 ? -cells[IX(i, 1, j)] : cells[IX(i, 1, j)];
                cells[IX(i, N, j)] = b == 2 ? -cells[IX(i, N - 1, j)] : cells[IX(i, N - 1, j)];

                //Forward and Aft bounds
                cells[IX(i, j, 0)] = b == 3 ? -cells[IX(i, j, 1)] : cells[IX(i, j, 1)];
                cells[IX(i, j, N)] = b == 3 ? -cells[IX(i, j, N - 1)] : cells[IX(i, j, N - 1)];
            }

            //Now the Corners
            cells[IX(0, 0, 0)] = 0.33f * (cells[IX(1, 0, 0)] + cells[IX(0, 1, 0)] + cells[IX(0, 0, 1)]);//Average of the neighboring cells
            cells[IX(0, 0, N)] = 0.33f * (cells[IX(1, 0, N)] + cells[IX(0, 1, N)] + cells[IX(0, 0, N - 1)]);
            cells[IX(0, N, 0)] = 0.33f * (cells[IX(1, N, 0)] + cells[IX(0, N - 1, 0)] + cells[IX(0, N, 1)]);
            cells[IX(N, 0, 0)] = 0.33f * (cells[IX(N - 1, 0, 0)] + cells[IX(N, 1, 0)] + cells[IX(N, 0, 1)]);

            cells[IX(N, N, N)] = 0.33f * (cells[IX(N - 1, N, N)] + cells[IX(N, N - 1, N)] + cells[IX(N, N, N - 1)]);
            cells[IX(N, N, 0)] = 0.33f * (cells[IX(N - 1, N, 0)] + cells[IX(N, N - 1, 0)] + cells[IX(N, N, 1)]);
            cells[IX(N, 0, N)] = 0.33f * (cells[IX(N - 1, 0, N)] + cells[IX(N, 1, N)] + cells[IX(N, 0, N - 1)]);
            cells[IX(0, N, N)] = 0.33f * (cells[IX(1, N, N)] + cells[IX(0, N - 1, N)] + cells[IX(0, N, N - 1)]);

        }
    }

    float GaussianKernel(float r, float h)
    {
        return (1 / (Mathf.Pow(Mathf.PI, 3 / 2) * Mathf.Pow(h, 3))) * Mathf.Exp((r * r) / (h * h));
    }
    public FluidParticle[] Particles
    {
        get { return particles; }
        set { particles = value; }
    }
    public FluidFieldRender Renderer
    {
        get { return fieldRender; }
    }

    public Vector3 POS(int index)
    {
        return new Vector3(index / (fieldRender.Size * fieldRender.Size), (index / fieldRender.Size) % fieldRender.Size, index % fieldRender.Size);
    }
    public int IX(float x, float y, float z)
    {
        return INDEX(new Vector3(x, y, z));
    }
    public int INDEX(float x, float y, float z)
    {
        return INDEX(new Vector3(x, y, z));
    }
    public int INDEX(Vector3 pos)
    {
        float width = fieldRender.Resolution.x;
        float height = fieldRender.Resolution.y;
        return (int)(pos.x + width * (pos.y + height * pos.z));
    }
    public void SWAP(float[] x, float[] x0)
    {
        float[] temp = x0;
        x0 = x;
        x = temp;
    }

    void OnDrawGizmos()
    {
        if (particles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                if (displayParticles)
                {
                    particles[i].DrawParticles(fieldRender.dimensions, fieldRender.resolution);
                }
                if (displayDensities)
                {
                    particles[i].DrawDensities(fieldRender.dimensions, fieldRender.resolution);
                }
                if (displayVelocites)
                {
                    particles[i].DrawVelocities(fieldRender.dimensions, fieldRender.resolution);
                }
            }
        }
    }

}
