/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neutron : MonoBehaviour
{
    [SerializeField] public const float MASS = 1837.74f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (GameObject particle in God.particles)
        {
            if (particle == gameObject) continue;

            if (particle.GetComponent<Electron>() != null)
            {
                God.ApplyGravitationalForce(particle, gameObject, Electron.MASS, MASS);
            }
            else if (particle.GetComponent<Proton>() != null)
            {
                God.ApplyGravitationalForce(particle, gameObject, Proton.MASS, MASS);
            }
            else if (particle.GetComponent<Neutron>() != null)
            {
                God.ApplyGravitationalForce(particle, gameObject, Neutron.MASS, MASS);
            }
        }
    }

    void ApplyGravitationalForce(GameObject particle, float particleMass)
    {
        // Gravitational force: ( GRAVITATIONAL_CONSTANT * mass(particle1) * mass(particle2) ) / distance^2

        float distance = Vector2.Distance(transform.position, particle.transform.position);

        float forceMagnitude = God.GRAVITATIONAL_CONSTANT * MASS * particleMass / Mathf.Pow(distance, 2);
        Vector2 force = (transform.position - particle.transform.position).normalized * forceMagnitude;


        particle.GetComponent<Rigidbody2D>().AddForce(force);
    }
}
*/