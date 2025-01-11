/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proton : MonoBehaviour
{
    [SerializeField] public const float CHARGE = God.BASE_CHARGE;
    [SerializeField] public const float MASS = 1836.23f;


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
                God.ApplyElectromagneticForce(particle, gameObject, Electron.CHARGE, CHARGE);
                God.ApplyGravitationalForce(particle, gameObject, Electron.MASS, MASS);
            }
            else if (particle.GetComponent<Proton>() != null)
            {
                God.ApplyElectromagneticForce(particle, gameObject, Proton.CHARGE, CHARGE);
                God.ApplyGravitationalForce(particle, gameObject, Proton.MASS, MASS);
            }
            else if (particle.GetComponent<Neutron>() != null)
            {
                God.ApplyGravitationalForce(particle, gameObject, Neutron.MASS, MASS);
            }
        }
    }
}
*/