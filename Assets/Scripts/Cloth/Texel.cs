using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Texel : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    float damping = 0;

#pragma warning disable CS0649 // Field 'Texel.rest_distance' is never assigned to, and will always have its default value 0
    int rest_distance;
#pragma warning restore CS0649 // Field 'Texel.rest_distance' is never assigned to, and will always have its default value 0

    Vector3 pos;
    Vector3 prevPos;
    Vector3 acceleration;

#pragma warning disable CS0649 // Field 'Texel.structConstraint' is never assigned to, and will always have its default value null
    Texel[] structConstraint;
#pragma warning restore CS0649 // Field 'Texel.structConstraint' is never assigned to, and will always have its default value null
#pragma warning disable CS0649 // Field 'Texel.shearConstraint' is never assigned to, and will always have its default value null
    Texel[] shearConstraint;
#pragma warning restore CS0649 // Field 'Texel.shearConstraint' is never assigned to, and will always have its default value null
#pragma warning disable CS0649 // Field 'Texel.bendContraint' is never assigned to, and will always have its default value null
    Texel[] bendContraint;
#pragma warning restore CS0649 // Field 'Texel.bendContraint' is never assigned to, and will always have its default value null

    // Update is called once per frame
    public void PhysicsUpdate()
    {
        Vector3 temp = pos;
        pos = pos + (pos - prevPos) * (1.0f - damping) + acceleration * Time.deltaTime;
        prevPos = temp;
        acceleration = new Vector3(0, 0, 0);

        for(int i =0;i<structConstraint.Length;i++)
        {

        }
        for (int i = 0; i < shearConstraint.Length; i++)
        {

        }
        for (int i = 0; i < bendContraint.Length; i++)
        {

        }
    }
    void satisfyConstraint(Texel p1, Texel p2)
    {
        Vector3 p1_to_p2 = p2.GetPos() - p1.GetPos(); // vector from p1 to p2
        float current_distance = p1_to_p2.magnitude;
        Vector3 correctionVector = p1_to_p2 * (1 - rest_distance / current_distance);
        Vector3 correctionVectorHalf = correctionVector * 0.5f;
        p1.OffsetPos(correctionVectorHalf);
        p2.OffsetPos(-correctionVectorHalf);
    }

    void addForce(Vector3 force, float mass)
    {
        acceleration += force / mass;
    }

    public Vector3 GetPos()
    {
        return pos;
    } 
    
    public void OffsetPos(Vector3 offset)
    {
        pos += offset;
    }
}
