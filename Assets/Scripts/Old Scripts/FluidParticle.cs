using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public struct Neighbor
//{
//    public Vector3 Position;
//    public Vector3 Velocity;
//    public Vector3 PrevVelocity;
//    public float PrevDensity;
//    public float Density;
//}

public struct FluidParticle
{
    public Vector3 position;

    float dt;
    float diff;
    float visc;

    float s;
    float density;

    float Vx;
    float Vy;
    float Vz;

    float Vx0;
    float Vy0;
    float Vz0;


    public void Initialize(float _timeStep, float _diff, float _visc)
    {
        dt = _timeStep;
        diff = _diff;
        visc = _visc;

        Vx = 0;
        Vy = 0;
        Vz = 0;

        Vx0 = Vx;
        Vy0 = Vy;
        Vz0 = Vz;



        #region notused
        /*
        Neighbors = new List<Neighbor>();
        if (Position.x > 0 && Position.x < FluidFieldRender.resolution.x && Position.y > 0 && Position.y < FluidFieldRender.resolution.y && Position.z > 0 && Position.z < FluidFieldRender.resolution.z)
        {
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(1, 0, 0))]);//MidMidLeft
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 1, 0))]);//MidUpperMid
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 0, 1))]);//FrontMidMid
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(-1, 0, 0))]);//MidMidRight
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, -1, 0))]);//MidLowerMid
            //Neighbors.Add(Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 0, -1))]);//BackMidMid

            FluidParticle particle;

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(1, 0, 0))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 1, 0))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 0, 1))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(-1, 0, 0))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, -1, 0))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });

            particle = Simulation.Particles[Simulation.INDEX(Position + new Vector3(0, 0, -1))];
            Neighbors.Add(new Neighbor()
            {
                Position = particle.Position,
                Velocity = particle.Velocity,
                PrevVelocity = particle.PrevVelocity,
                Density = particle.Density,
                PrevDensity = particle.PrevDensity
            });
        }
        */
        #endregion
    }

    public void UpdateParticle(float _timeStep, float _diff, float _visc)
    {
        dt = _timeStep;
        diff = _diff;
        visc = _visc;

        Vx0 = Vx;
        Vy0 = Vy;
        Vz0 = Vz;

        //Vy += -9.8f * Time.deltaTime;

        density = s;
    }



    public void SetVelocity(Vector3 vec)
    {
        Vx = vec.x;
        Vy = vec.y;
        Vz = vec.z;
    }
    public void SetPrevVelocity(Vector3 vec)
    {
        Vx0 = vec.x;
        Vy0 = vec.y;
        Vz0 = vec.z;
    }
    public void SetDensity(float d)
    {
        density = d;
    }
    public void SetPrevDensity(float d)
    {
        s = d;
    }

    public void AddVelocity(Vector3 vec)
    {
        Vx += vec.x;
        Vy += vec.y;
        Vz += vec.z;
    }
    public void AddPrevVelocity(Vector3 vec)
    {
        Vx0 += vec.x;
        Vy0 += vec.y;
        Vz0 += vec.z;
    }
    public void AddDensity(float d)
    {
        density += d;
    }
    public void AddPrevDensity(float d)
    {
        s += d;
    }

    public Vector3 GetVelocityX()
    {
        return new Vector3(Vx, 0, 0);
    }
    public Vector3 GetVelocityY()
    {
        return new Vector3(0, Vy, 0);
    }
    public Vector3 GetVelocityZ()
    {
        return new Vector3(0, 0, Vz);
    }
    public Vector3 GetPrevVelocityX()
    {
        return new Vector3(Vx0, 0, 0);
    }
    public Vector3 GetPrevVelocityY()
    {
        return new Vector3(0, Vy, 0);
    }
    public Vector3 GetPrevVelocityZ()
    {
        return new Vector3(0, 0, Vz0);
    }

    public float GetDensity()
    {
        return density;
    }
    public float GetPrevDensity()
    {
        return s;
    }

    public void DrawParticles(Vector3 dimensions, Vector3 resolution)
    {
        Vector3 Scale = new Vector3(dimensions.x / resolution.x, dimensions.y / resolution.y, dimensions.z / resolution.z);
        //Gizmos.color = new Color((density + .18f) % 1, .78f, density);
        Gizmos.color = new Color(density, 0, 1 - density,density);
        Vector3 actualPos = new Vector3(position.x * (Scale.x), position.y * (Scale.y), position.z * (Scale.z));
        Gizmos.DrawSphere(actualPos, Scale.x / 5f);
    }
    public void DrawMesh(Mesh mesh, Material material, Vector3 dimensions, Vector3 resolution)
    {
        Vector3 Scale = new Vector3(dimensions.x / resolution.x, dimensions.y / resolution.y, dimensions.z / resolution.z);
        Gizmos.color = new Color(density, 0, 0, density);
        Vector3 actualPos = new Vector3(position.x * (Scale.x), position.y * (Scale.y), position.z * (Scale.z));
        material.color = Gizmos.color;
        mesh.bounds = new Bounds(Vector3.zero, Scale);
        Graphics.DrawMesh(mesh, actualPos, Quaternion.identity, material, 0);
    }
    public void DrawDensities(Vector3 dimensions, Vector3 resolution)
    {
        Vector3 Scale = new Vector3(dimensions.x / resolution.x, dimensions.y / resolution.y, dimensions.z / resolution.z);
        Vector3 actualPos = new Vector3(position.x * (Scale.x), position.y * (Scale.y), position.z * (Scale.z));
        GizmoUtils.DrawText(GUI.skin, String.Format("{0:0.00}", density), actualPos, Color.blue, 20, 0.5f);
    }
    public void DrawVelocities(Vector3 dimensions, Vector3 resolution)
    {
        if (Vx != 0 || Vy != 0 || Vz != 0)
        {
            Vector3 Scale = new Vector3(dimensions.x / resolution.x, dimensions.y / resolution.y, dimensions.z / resolution.z);
            Gizmos.color = new Color(0, 1, 0);
            Vector3 actualPos = new Vector3(position.x * (Scale.x), position.y * (Scale.y), position.z * (Scale.z));
            Debug.DrawRay(actualPos, new Vector3(Vx, Vy, Vz).normalized * .1f, Gizmos.color);
        }
    }

    Vector3 Clamp(Vector3 min, Vector3 max, Vector3 input)
    {
        Vector3 output;
        output.x = Mathf.Clamp(input.x, min.x, max.x);
        output.y = Mathf.Clamp(input.y, min.y, max.y);
        output.z = Mathf.Clamp(input.z, min.z, max.z);
        return output;
    }

    public string Print()
    {
        return "Vx" + Vx + "\nVy"
                + Vy + "\nVz"
                + Vz + "\nVx0"
                + Vx0 + "\nVy0"
                + Vy0 + "\nVz0"
                + Vz0;
    }
}
