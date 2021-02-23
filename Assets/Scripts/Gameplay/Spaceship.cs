using Photon.Compression;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.InputSystem;

//Maybe write own client side prediction: https://www.kinematicsoup.com/news/2017/5/30/multiplayerprediction

public interface IDamagable
{
    void TakeDamage(Bullet bullet, float amount);
}

public class Spaceship : MonobehaviourPunPew, IDamagable, IPunObservable
{
    [SerializeField] float engineForce, rotationForce, breakForce, panForce;
    [SerializeField] float maxHealth;
    [SerializeField] float rotationCompensationMultiplyer;
    [SerializeField] float maxSpeed, maxRotationSpeed;

    [Header("Components")]
    [SerializeField] SpriteRenderer teamColorsRenderer;
    [SerializeField] Transform cockpitCamTarget, weapon1CamTarget;
    [SerializeField] new Rigidbody2D rigidbody;
    [SerializeField] new Light2D light, directionalLight;
    [SerializeField] ParticleSystem enginePS;
    [SerializeField] Transform engineTransform;
    [SerializeField] Gun[] pilotGuns;


    [SerializeField] ParticleSystem rcsLeftFront, rcsLeftBack, rcsRightFront, rcsRightBack, rcsFront;

    private Team owningTeam;
    private float health;

    private float verticalCache;
    private float horizontalCache;
    private float panCache;
    private float rotationCompensation;

    private Vector2 movementForce;
    private Vector3 directionalLightOffset;
    private Quaternion directionalLightRotation;

    private bool complexControlScheme = false;

    public System.Action<Spaceship> HealthChanged;

    public Team Team { get => owningTeam; }

    public float Health { get => health; }

    public float MaxHealth { get => maxHealth; }

    public Rigidbody2D Rigidbody { get => rigidbody; }

    protected override void OnAllReady()
    {
        health = maxHealth;
    }

    protected override void Start()
    {
        base.Start();
        directionalLightOffset = directionalLight.transform.localPosition;
        directionalLightRotation = directionalLight.transform.rotation;
    }

    private void Update()
    {
        if (AmOwningPilot())
        {
            if (photonView.IsMine)
            {
                PilotUpdate();
            }
            else
            {
                //dont have ownership even though I think I should, requesting it
                Debug.Log("Ownership request for spaceship control by " + PhotonNetwork.LocalPlayer.NickName);
                photonView.RequestOwnership();
            }
        }

        UpdateEffects();
    }

    private bool AmOwningPilot()
    {
        return owningTeam == GetLocalTeam() && GetLocalPlayerRole() == PlayerRole.Pilot;
    }

    private void UpdateEffects()
    {
        directionalLight.transform.position = transform.position + directionalLightOffset;
        directionalLight.transform.rotation = directionalLightRotation;

        if (complexControlScheme)
        {
            //engines breaks
            var emission = enginePS.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(10, 2000, Mathf.Max(0, verticalCache));

            emission = rcsFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, -verticalCache)));

            //front 
            emission = rcsLeftFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(horizontalCache, -rotationCompensation / 20, panCache)));

            emission = rcsRightFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(-horizontalCache, rotationCompensation / 20, -panCache)));

            //back
            emission = rcsLeftBack.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, panCache)));

            emission = rcsRightBack.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, -panCache)));
        }
        else
        {
            float engineStrength = Vector3.Dot(-transform.up, movementForce);
            float leftStrength = Vector3.Dot(-transform.right, movementForce);
            float rightStrength = Vector3.Dot(transform.right, movementForce);
            float topStrength = Vector3.Dot(transform.up, movementForce);

            //engines breaks
            var emission = enginePS.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(10, 2000, Mathf.Max(0, engineStrength));

            emission = rcsFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, topStrength)));

            //front 
            emission = rcsLeftFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, leftStrength)));

            emission = rcsRightFront.emission;
            emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, rightStrength)));
        }
    }

    private void PilotUpdate()
    {
        if (complexControlScheme)
            ComplexControlSchemeUpdate();
        else
            SimpleControlSchemeUpdate();
    }

    private void ComplexControlSchemeUpdate()
    {
        //accell + breaks RCS
        if (verticalCache >= 0)
            rigidbody.AddForce(transform.up * Mathf.Min(-0.05f, -verticalCache) * engineForce * Time.deltaTime);
        else
            rigidbody.AddForce(transform.up * -verticalCache * breakForce * Time.deltaTime);

        rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);

        //panning RCS
        rigidbody.AddForce(transform.right * -panCache * panForce * Time.deltaTime);

        //directional assistant

        //roation compensation RCS
        if (Mathf.Abs(horizontalCache) < 0.1f)
            rotationCompensation = -rigidbody.angularVelocity;
        else
            rotationCompensation = 0;
        rigidbody.AddTorque(rotationCompensation * rotationCompensationMultiplyer * Time.deltaTime);

        //rotation
        rigidbody.AddTorque(rotationForce * -horizontalCache * Time.deltaTime);
        rigidbody.angularVelocity = Mathf.Clamp(rigidbody.angularVelocity, -maxRotationSpeed, maxRotationSpeed);
    }

    private void SimpleControlSchemeUpdate()
    {
        Vector2 target = new Vector2(horizontalCache, verticalCache).normalized * maxSpeed;
        Vector2 velocity = rigidbody.velocity;

        Vector2 forceDir = (target - velocity).normalized;

        movementForce = forceDir;

        rigidbody.AddForce(forceDir * engineForce * Time.deltaTime);

        if (velocity.magnitude > 0.5f)
            transform.up = -rigidbody.velocity;
    }

    public void Server_SetTeam(Team team)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SetTeam), RpcTarget.AllBuffered, team);
        }
    }

    [PunRPC]
    private void RPC_SetTeam(Team team)
    {
        Debug.Log("RPC: SetTeam " + team);

        owningTeam = team;
        teamColorsRenderer.color = team.ToColor();

        ServiceLocator.SetSpaceship(team, this);
        ServiceLocator.SetLocation(team, Location.Cockpit, cockpitCamTarget);
        ServiceLocator.SetLocation(team, Location.Weapon1, weapon1CamTarget);

        light.gameObject.SetActive(team == GetLocalTeam());
        directionalLight.gameObject.SetActive(team == GetLocalTeam());
    }

    private void Server_SetHealth(float newHealth)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SetHealth), RpcTarget.All, newHealth);
        }
        else
        {
            Debug.LogWarning(name + " T: " + Team + " Trying to set health not on MasterClient.");
        }
    }

    [PunRPC]
    private void RPC_SetHealth(float newHealth)
    {
        health = newHealth;
        HealthChanged?.Invoke(this);
    }

    public void TakeDamage(Bullet bullet, float amount)
    {
        if (bullet.Team != owningTeam)
        {
            Server_SetHealth(health - amount);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(horizontalCache);
            stream.SendNext(verticalCache);
            stream.SendNext(rotationCompensation);
            stream.SendNext(panCache);
        }
        else
        {
            horizontalCache = (float)stream.ReceiveNext();
            verticalCache = (float)stream.ReceiveNext();
            rotationCompensation = (float)stream.ReceiveNext();
            panCache = (float)stream.ReceiveNext();
        }
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (AmOwningPilot())
        {
            var value = context.ReadValue<Vector2>();
            verticalCache = value.y;
            horizontalCache = value.x;
        }
    }

    public void OnPanInput(InputAction.CallbackContext context)
    {
        if (AmOwningPilot())
        {
            var value = context.ReadValue<float>();
            panCache = value;
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (AmOwningPilot())
        {
            if (context.started)
            {
                foreach (var gun in pilotGuns)
                {
                    gun.StartFiring(Rigidbody, Team);
                }
            }
            else if (context.canceled)
            {
                foreach (var gun in pilotGuns)
                {
                    gun.StopFiring();
                }
            }
        }
    }
}