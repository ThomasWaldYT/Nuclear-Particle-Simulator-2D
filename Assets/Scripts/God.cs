using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.UIElements;

public class God : MonoBehaviour
{
    public ComputeShader particleComputeShader; // Reference to the compute shader
    private ParticleSystem godParticleSystem;   // Reference to the particle system

    // Particle properties
    public float electronSize;
    public float protonSize;
    public float neutronSize;
    public float photonSize;

    public float electronMass;
    public float protonMass;
    public float neutronMass;

    public float electronCharge;
    public float protonCharge;
    public float neutronCharge;

    public Color electronColor;
    public Color protonColor;
    public Color neutronColor;
    public Color photonColor;

    public float gravitationalConstant;
    public float coulombConstant;

    // Strong force parameters
    public float strongForceConstant;
    public float strongForceInnerRadius;
    public float strongForceOuterRadius;

    // Universal radius for particle wrapping
    public float universalRadius;

    // Drag coefficient
    public float dragCoefficient;

    // Photon properties
    public float speedOfLight;
    public float photonEmissionThreshold;
    public float maxPhotonEnergy;

    public float particleLifetime;

    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer massesBuffer;
    private ComputeBuffer chargesBuffer;
    private ComputeBuffer typesBuffer;
    private ComputeBuffer outVelocitiesBuffer;
    private ComputeBuffer activeParticleIndicesBuffer;
    private ComputeBuffer debugBuffer;

    private ParticleSystem.Particle[] particles;
    private Vector3[] positions;
    private Vector3[] velocities;
    private float[] masses;
    private float[] charges;
    private int[] types;
    private int[] activeParticleIndices;
    private float[] debug;

    private int bufferCapacity; // Tracks the current capacity of buffers

    public enum ParticleType { Electron = 0, Proton = 1, Neutron = 2, Photon = 3 }

    // Dictionary to map particle randomSeed to custom data
    private Dictionary<uint, CustomData> particleCustomData = new Dictionary<uint, CustomData>();

    private struct CustomData
    {
        public Color color;
        public float size;
        public float mass;
        public float charge;
        public int type;
        public float photonEnergy;
    }

    // Photon management
    public int maxPhotons = 1000;
    private int currentPhotons = 0;


    // God abilities
    private bool godGravityActivated = false;
    private bool godPositiveElectrostaticActivated = false;
    private bool godNegativeElectrostaticActivated = false;

    public float godMass;
    public float godCharge;


    private void Awake()
    {
        godParticleSystem = GetComponent<ParticleSystem>();
    }

    void Start()
    {
        int maxParticles = 10000; // Set your desired maximum capacity
        InitializeBuffers(maxParticles);
    }

    void Update()
    {
        // Particles will continuously spawn at the current mouse position when keys are held down
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Spawn Electrons when E key is held down
        if (Input.GetKeyDown(KeyCode.E))
            EmitParticle(electronSize, electronMass, electronCharge, electronColor, ParticleType.Electron, mousePosition, Vector2.zero);

        // Spawn Protons when P key is held down
        if (Input.GetKeyDown(KeyCode.P))
            EmitParticle(protonSize, protonMass, protonCharge, protonColor, ParticleType.Proton, mousePosition, Vector2.zero);

        // Spawn Neutrons when N key is held down
        if (Input.GetKeyDown(KeyCode.N))
            EmitParticle(neutronSize, neutronMass, neutronCharge, neutronColor, ParticleType.Neutron, mousePosition, Vector2.zero);

        godGravityActivated = Input.GetKey(KeyCode.Alpha1);
        godPositiveElectrostaticActivated = Input.GetKey(KeyCode.Alpha2);
        godNegativeElectrostaticActivated = Input.GetKey(KeyCode.Alpha3);
    }

    private void EmitParticle(float size, float mass, float charge, Color color, ParticleType type, Vector2 position, Vector2 velocity)
    {
        uint randomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

        var emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startColor = color,
            startSize = size,
            startLifetime = particleLifetime,
            randomSeed = randomSeed,
            applyShapeToPosition = true
        };
        godParticleSystem.Emit(emitParams, 1);

        CustomData customData = new CustomData
        {
            color = color,
            size = size,
            mass = mass,
            charge = charge,
            type = (int)type,
            photonEnergy = 0
        };

        particleCustomData[randomSeed] = customData;
    }

    private void EmitPhoton(Vector2 position, Vector2 photonVelocity, float energy)
    {
        if (photonVelocity == Vector2.zero)
        {
            Vector2 randomDirection = UnityEngine.Random.onUnitSphere;
            randomDirection.Normalize();
            photonVelocity = randomDirection * speedOfLight;
        }

        // Give the photon its color based on its energy
        Color color = photonColor;
        Color.RGBToHSV(color, out float H, out float S, out float V);

        // Convert the energy to a hue value of 1 - 300, then convert that to a fraction of 360 (the maximum hue value)
        H = (energy / maxPhotonEnergy) * 300 / 360;

        color = Color.HSVToRGB(H, S, V);

        uint randomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

        var emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = photonVelocity,
            startColor = color,
            startSize = photonSize,
            startLifetime = particleLifetime,
            randomSeed = randomSeed,
            applyShapeToPosition = true
        };
        godParticleSystem.Emit(emitParams, 1);

        CustomData customData = new CustomData
        {
            color = photonColor,
            size = 0,
            mass = 0f,
            charge = 0f,
            type = (int)ParticleType.Photon,
            photonEnergy = energy
        };

        particleCustomData[randomSeed] = customData;
    }

    void FixedUpdate()
    {
        int startingParticleCount = godParticleSystem.particleCount;
        if (startingParticleCount == 0) return;

        // Ensure particle count does not exceed buffer capacity
        if (startingParticleCount > bufferCapacity)
        {
            Debug.LogError("Particle count exceeds buffer capacity. Increase the maximum capacity.");
            return;
        }

        // Retrieve particles
        godParticleSystem.GetParticles(particles, startingParticleCount);

        // Build arrays for masses, charges, types, etc.
        int activeParticleCount = 0;

        for (int i = 0; i < startingParticleCount; i++)
        {
            positions[i] = particles[i].position;

            // Store current velocities before they get updated
            velocities[i] = particles[i].velocity;

            uint randomSeed = particles[i].randomSeed;
            if (particleCustomData.TryGetValue(randomSeed, out CustomData customData))
            {
                masses[i] = customData.mass;
                charges[i] = customData.charge;
                types[i] = customData.type;
            }
            else
            {
                // Assign default values or handle missing data
                masses[i] = 1f;
                charges[i] = 0f;
                types[i] = (int)ParticleType.Electron;
            }
            
            // Build active particle indices
            if (types[i] != (int)ParticleType.Photon)
            {
                activeParticleIndices[activeParticleCount] = i;
                activeParticleCount++;
            }
        }


        // Set data for compute shader
        positionsBuffer.SetData(positions, 0, 0, startingParticleCount);
        velocitiesBuffer.SetData(velocities, 0, 0, startingParticleCount);
        massesBuffer.SetData(masses, 0, 0, startingParticleCount);
        chargesBuffer.SetData(charges, 0, 0, startingParticleCount);
        typesBuffer.SetData(types, 0, 0, startingParticleCount);

        activeParticleIndicesBuffer.SetData(activeParticleIndices, 0, 0, activeParticleCount);

        // Set compute shader buffers and parameters
        particleComputeShader.SetBuffer(0, "positions", positionsBuffer);
        particleComputeShader.SetBuffer(0, "velocities", velocitiesBuffer);
        particleComputeShader.SetBuffer(0, "masses", massesBuffer);
        particleComputeShader.SetBuffer(0, "charges", chargesBuffer);
        particleComputeShader.SetBuffer(0, "types", typesBuffer);
        particleComputeShader.SetBuffer(0, "activeParticleIndices", activeParticleIndicesBuffer);
        particleComputeShader.SetBuffer(0, "outVelocities", outVelocitiesBuffer);
        particleComputeShader.SetBuffer(0, "debug", debugBuffer);

        particleComputeShader.SetInt("startingParticleCount", startingParticleCount);
        particleComputeShader.SetInt("activeParticleCount", activeParticleCount);
        particleComputeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        particleComputeShader.SetFloat("coulombConstant", coulombConstant);
        particleComputeShader.SetFloat("gravityConstant", gravitationalConstant);
        particleComputeShader.SetFloat("strongForceConstant", strongForceConstant);
        particleComputeShader.SetFloat("strongForceInnerRadius", strongForceInnerRadius);
        particleComputeShader.SetFloat("strongForceOuterRadius", strongForceOuterRadius);
        particleComputeShader.SetFloat("universalRadius", universalRadius);
        particleComputeShader.SetFloat("dragCoefficient", dragCoefficient);

        // Dispatch compute shader
        int threadGroups = Mathf.CeilToInt(startingParticleCount / 256.0f);
        particleComputeShader.Dispatch(0, threadGroups, 1, 1);

        // Get updated velocities
        outVelocitiesBuffer.GetData(velocities, 0, 0, startingParticleCount);

        //debugBuffer.GetData(debug, 0, 0, startingParticleCount);

        bool emittedParticleThisFrame = false;

        bool[] particleWasDeleted = new bool[startingParticleCount];

        for (int i = 0; i < startingParticleCount; i++)
        {
            // Retrieve custom data
            uint randomSeed = particles[i].randomSeed;
            if (!particleCustomData.TryGetValue(randomSeed, out CustomData customData)) continue;

            ParticleType type = (ParticleType)customData.type;

            // Check if the increase exceeds the threshold and the particle is not a photon
            if (velocities[i].magnitude > speedOfLight && type != ParticleType.Photon && currentPhotons < maxPhotons)
            {
                float excessVelocity = velocities[i].magnitude - speedOfLight;

                float energyLost = Mathf.Min(0.5f * customData.mass * Mathf.Pow(excessVelocity, 2), maxPhotonEnergy);


                Vector2 predictedPosition = particles[i].position + velocities[i].normalized * speedOfLight * Time.fixedDeltaTime;
                // Emit a photon at the particle's predicted position
                EmitPhoton(predictedPosition, Vector2.zero, energyLost);
                currentPhotons++;
                emittedParticleThisFrame = true;
            }


            // Check for influence of God abilities
            if (godGravityActivated && type != ParticleType.Photon)
            {
                Vector2 addedVelocity = GodGravityAbility(i);
                velocities[i] = new Vector2(velocities[i].x + addedVelocity.x, velocities[i].y + addedVelocity.y);
            }
            if ((godPositiveElectrostaticActivated || godNegativeElectrostaticActivated) && type != ParticleType.Photon && type != ParticleType.Neutron)
            {
                Vector2 addedVelocity = GodElectrostaticAbility(i);
                velocities[i] = new Vector2(velocities[i].x + addedVelocity.x, velocities[i].y + addedVelocity.y);
            }

            // Apply drag for nucleons or detect collisions with other non-photon particles for photons
            if (type != ParticleType.Photon) velocities[i] *= (1.0f - dragCoefficient * Time.fixedDeltaTime);
            else particleWasDeleted[i] = CheckPhotonCollisions(i, startingParticleCount, ref velocities);

            // Clamp velocities to the speed of light
            if (velocities[i].magnitude > speedOfLight) velocities[i] = velocities[i].normalized * speedOfLight;

            // Update position
            Vector3 newPosition = particles[i].position + velocities[i] * Time.fixedDeltaTime;

            // Correct wrapping logic
            if (newPosition.magnitude > universalRadius)
            {
                WrapParticle(i, newPosition, velocities[i]);
                particleWasDeleted[i] = true;
                emittedParticleThisFrame = true;
            }

            // Ensure particle stays in the XY plane
            newPosition.z = 0f;

            positions[i] = newPosition;
        }

        if (emittedParticleThisFrame)
        {
            // Update particle count and refresh the particles array
            godParticleSystem.GetParticles(particles, godParticleSystem.particleCount);
        }

        // Apply updated velocities and positions to old particles (not new ones)
        for (int i = 0; i < startingParticleCount; i++)
        {
            if (particleWasDeleted[i]) particles[i].remainingLifetime = 0;
            else
            {
                // Clamp velocities to the speed of light
                if (velocities[i].magnitude > speedOfLight) velocities[i] = velocities[i].normalized * speedOfLight;
                particles[i].velocity = velocities[i];
                particles[i].position = positions[i];
            }
        }

        godParticleSystem.SetParticles(particles, godParticleSystem.particleCount);
    }

    private Vector2 GodGravityAbility(int index)
    {
        if (!particleCustomData.TryGetValue(particles[index].randomSeed, out CustomData cd)) return Vector2.zero;

        float distance = Vector2.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), particles[index].position);
        Vector2 direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - particles[index].position).normalized;
        float gravitationalForce = (gravitationalConstant * godMass * cd.mass) / Mathf.Pow(distance, 2);
        Vector2 acceleration = direction * gravitationalForce / cd.mass;

        return acceleration * Time.fixedDeltaTime;
    }

    private Vector2 GodElectrostaticAbility(int index)
    {
        if (!particleCustomData.TryGetValue(particles[index].randomSeed, out CustomData cd)) return Vector2.zero;

        float distance = Vector2.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), particles[index].position);
        Vector2 direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - particles[index].position).normalized;

        int sign = godPositiveElectrostaticActivated ? 1 : -1;
        float electrostaticForce = -(coulombConstant * godCharge * sign * cd.charge) / Mathf.Pow(distance, 2);
        Vector2 acceleration = direction * electrostaticForce / cd.mass;

        return acceleration * Time.fixedDeltaTime;
    }

    private bool CheckPhotonCollisions(int index, int startingParticleCount, ref Vector3[] velocities)
    {
        for (int i = 0; i < startingParticleCount; i++)
        {
            if (!particleCustomData.TryGetValue(particles[i].randomSeed, out CustomData cd)) continue;
            if (cd.type == (int)ParticleType.Photon) continue;

            if (Vector2.Distance(particles[index].position, particles[i].position) <= cd.size / 2)
            {
                // Add energy of photon to the particle that was collided with
                CustomData photonCD = particleCustomData[particles[index].randomSeed];
                Vector2 photonEnergyVector = particles[index].velocity.normalized * photonCD.photonEnergy;
                Vector2 particleEnergyVector = particles[i].velocity.normalized * 0.5f * cd.mass * Mathf.Pow(particles[i].velocity.magnitude, 2);

                particleEnergyVector += photonEnergyVector;

                velocities[i] = particleEnergyVector.normalized * (Mathf.Sqrt(2 * particleEnergyVector.magnitude / cd.mass));

                // Delete photon
                particleCustomData.Remove(particles[index].randomSeed);
                currentPhotons--;
                return true;
            }
        }

        return false;
    }

    private void WrapParticle(int index, Vector2 position, Vector2 velocity)
    {
        if (!particleCustomData.TryGetValue(particles[index].randomSeed, out CustomData cd)) return;

        // Calculate the excess distance beyond the universal radius
        float excessDistance = position.magnitude - universalRadius;

        // Normalize the direction
        Vector2 direction = position.normalized;

        // Calculate the new position on the opposite side
        position = -direction * universalRadius + direction * excessDistance;

        if (cd.type != (int)ParticleType.Photon) EmitParticle(cd.size, cd.mass, cd.charge, cd.color, (ParticleType)cd.type, position, velocity);
        else EmitPhoton(position, velocity, cd.photonEnergy);

        particleCustomData.Remove(particles[index].randomSeed);
    }

    private void InitializeBuffers(int capacity)
    {
        bufferCapacity = capacity;

        positionsBuffer = new ComputeBuffer(bufferCapacity, sizeof(float) * 3);
        velocitiesBuffer = new ComputeBuffer(bufferCapacity, sizeof(float) * 3);
        massesBuffer = new ComputeBuffer(bufferCapacity, sizeof(float));
        chargesBuffer = new ComputeBuffer(bufferCapacity, sizeof(float));
        typesBuffer = new ComputeBuffer(bufferCapacity, sizeof(int));
        outVelocitiesBuffer = new ComputeBuffer(bufferCapacity, sizeof(float) * 3);
        activeParticleIndicesBuffer = new ComputeBuffer(bufferCapacity, sizeof(int));
        debugBuffer = new ComputeBuffer(bufferCapacity, sizeof(float));

        positions = new Vector3[bufferCapacity];
        velocities = new Vector3[bufferCapacity];
        masses = new float[bufferCapacity];
        charges = new float[bufferCapacity];
        types = new int[bufferCapacity];
        particles = new ParticleSystem.Particle[bufferCapacity];
        activeParticleIndices = new int[bufferCapacity];
        debug = new float[bufferCapacity];
    }

    private void ReleaseBuffers()
    {
        if (positionsBuffer != null) positionsBuffer.Release();
        if (velocitiesBuffer != null) velocitiesBuffer.Release();
        if (massesBuffer != null) massesBuffer.Release();
        if (chargesBuffer != null) chargesBuffer.Release();
        if (typesBuffer != null) typesBuffer.Release();
        if (outVelocitiesBuffer != null) outVelocitiesBuffer.Release();
        if (activeParticleIndicesBuffer != null) activeParticleIndicesBuffer.Release();
        if (debugBuffer != null) debugBuffer.Release();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnDrawGizmos()
    {
        Handles.color = Color.green;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, universalRadius);
    }
}