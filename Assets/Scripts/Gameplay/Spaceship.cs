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
    [SerializeField] float engineForce, rotationMultiplyer, breakForce, panForce;
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
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Gun[] pilotGuns;


    [SerializeField] ParticleSystem rcsLeftFront, rcsLeftBack, rcsRightFront, rcsRightBack, rcsFront;

    private Team owningTeam;
    private float health;

    private float verticalCache;
    private float horizontalCache;
    private float panCache;
    private float acceleration;
    private float rotationForce;

    private Vector2 movementTarget;
    private Vector3 directionalLightOffset;
    private Quaternion directionalLightRotation;

    [SerializeField] bool complexControlScheme = false;

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
        playerInput.SwitchCurrentControlScheme(Gamepad.current);

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


        //engines breaks
        var emission = enginePS.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(10, 2000, Mathf.Max(0, acceleration));

        //emission = rcsFront.emission;
        //emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, topStrength)));

        //front 
        emission = rcsLeftFront.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, -rotationForce * 0.1f, panCache)));

        emission = rcsRightFront.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, rotationForce * 0.1f, -panCache)));

        //back
        emission = rcsLeftBack.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, panCache)));

        emission = rcsRightBack.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(0, -panCache)));

    }

    private void PilotUpdate()
    {
        SimpleControlSchemeUpdate();
    }

    private void SimpleControlSchemeUpdate()
    {
        movementTarget = new Vector2(horizontalCache, verticalCache);
        if (movementTarget.magnitude > 1)
            movementTarget.Normalize();
        Vector2 current = -transform.up;

        var angleLeft = Vector2.SignedAngle(current, movementTarget);
        var rotSpeed = rigidbody.angularVelocity;
        rotationForce = angleLeft*2 - rotSpeed;
        rigidbody.AddTorque(rotationForce * rotationMultiplyer * Time.deltaTime);
        rigidbody.angularVelocity = Mathf.Clamp(rigidbody.angularVelocity, -maxRotationSpeed, maxRotationSpeed);

        //force
        rigidbody.AddForce(-transform.up * acceleration * engineForce * Time.deltaTime);

        rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);

        //panning RCS
        rigidbody.AddForce(transform.right * -panCache * panForce * Time.deltaTime);
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
            stream.SendNext(panCache);
            stream.SendNext(acceleration);
            stream.SendNext(rotationForce);
        }
        else
        {
            horizontalCache = (float)stream.ReceiveNext();
            verticalCache = (float)stream.ReceiveNext();
            panCache = (float)stream.ReceiveNext();
            acceleration = (float)stream.ReceiveNext();
            rotationForce = (float)stream.ReceiveNext();
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
    public void OnAccelerate(InputAction.CallbackContext context)
    {
        if (AmOwningPilot())
        {
            acceleration = context.ReadValue<float>();
        }
    }
}