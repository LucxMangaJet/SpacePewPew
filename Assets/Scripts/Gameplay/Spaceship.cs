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
//add boost

public interface IDamagable
{
    void TakeDamage(Bullet bullet, float amount);
}

public class Spaceship : MonobehaviourPunPew, IDamagable, IPunObservable
{
    [SerializeField] float engineForce, breakForce, rcsPanThrusterForce, sasHorizontalAssistMultiplyer;
    [SerializeField] float maxHealth;
    [SerializeField] float rotationRetargetingMultiplyer;
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


    [SerializeField] ParticleSystem rcsRotationLeft, rcsPanLeft, rcsRotationRight, rcsPanRight, rcsFront, sasLeft, sasRight;

    private Team owningTeam;
    private float health;

    private float verticalCache;
    private float horizontalCache;
    private float panCache;
    private float accelerationCache;
    private float breaksCache;
    private float rotationForce;
    private float sasStrength;

    private Vector2 movementTarget;
    private Vector3 directionalLightOffset;
    private Quaternion directionalLightRotation;

    public event System.Action<Spaceship> HealthChanged;
    public event System.Action<Spaceship> Destroyed;

    public Team Team { get => owningTeam; }

    public float Health { get => health; }

    public float MaxHealth { get => maxHealth; }

    public Rigidbody2D Rigidbody { get => rigidbody; }

    protected override void Start()
    {
        playerInput.SwitchCurrentControlScheme(Gamepad.current);

        base.Start();
        directionalLightOffset = directionalLight.transform.localPosition;
        directionalLightRotation = directionalLight.transform.rotation;

        health = maxHealth;
        HealthChanged?.Invoke(this);
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


        //engines 
        var emission = enginePS.emission;
        emission.rateOverTimeMultiplier = Mathf.LerpUnclamped(10, 2000, Mathf.Max(0, accelerationCache));

        //breakes
        emission = rcsFront.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, breaksCache * breakForce);

        //RCS Rotation 
        emission = rcsRotationLeft.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, -rotationForce);

        emission = rcsRotationRight.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, rotationForce);

        //RCS Pan
        emission = rcsPanLeft.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, panCache * rcsPanThrusterForce);

        emission = rcsPanRight.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, -panCache * rcsPanThrusterForce);

        //SAS Horizontal assist
        emission = sasLeft.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, -sasStrength);

        emission = sasRight.emission;
        emission.rateOverTimeMultiplier = Mathf.Max(0, sasStrength);

    }

    private void PilotUpdate()
    {
        SimpleControlSchemeUpdate();
    }

    private void SimpleControlSchemeUpdate()
    {
        //targeting
        Vector2 newTarget = new Vector2(horizontalCache, verticalCache);
        if (newTarget.magnitude >= 1)
            newTarget.Normalize();
        if (newTarget.magnitude >= 0.1f)
            movementTarget = Vector3.Slerp(movementTarget, newTarget, newTarget.magnitude * Time.deltaTime * rotationRetargetingMultiplyer);
        Vector2 current = -transform.up;

        var angleLeft = Vector2.SignedAngle(current, movementTarget);
        var rotSpeed = rigidbody.angularVelocity;
        rotationForce = GetRotationForceToSolve(angleLeft, rotSpeed, 0.5f);
        rigidbody.AddTorque(rotationForce * Time.deltaTime);
        rigidbody.angularVelocity = Mathf.Clamp(rigidbody.angularVelocity, -maxRotationSpeed, maxRotationSpeed);

        //engines
        rigidbody.AddForce(-transform.up * accelerationCache * engineForce * Time.deltaTime);

        //breakes
        rigidbody.AddForce(transform.up * breaksCache * breakForce * Time.deltaTime);

        if (Mathf.Abs(panCache) > 0.1f)
        {
            //panning RCS
            rigidbody.AddForce(transform.right * -panCache * rcsPanThrusterForce * Time.deltaTime);
        }
        else
        {
            //autocorrection SAS
            sasStrength = -Vector2.Dot(transform.right, rigidbody.velocity) * sasHorizontalAssistMultiplyer;
            rigidbody.AddForce(transform.right * sasStrength * Time.deltaTime);
        }

        rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);
    }

    private float GetRotationForceToSolve(float angleLeft, float currentRotSpeed, float time)
    {
        //aleft = t*s0 + 0.5f*a*t*t
        //a = (aleft - t*s0)/(0.5f*t*t)

        float dividend = angleLeft - time * currentRotSpeed;
        float divisor = 0.5f * time * time;
        return dividend / divisor;
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

        if(health <= 0)
        {
            Destroyed?.Invoke(this);
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
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
            stream.SendNext(accelerationCache);
            stream.SendNext(breaksCache);
            stream.SendNext(rotationForce);
            stream.SendNext(sasStrength);
        }
        else
        {
            horizontalCache = (float)stream.ReceiveNext();
            verticalCache = (float)stream.ReceiveNext();
            panCache = (float)stream.ReceiveNext();
            accelerationCache = (float)stream.ReceiveNext();
            breaksCache = (float)stream.ReceiveNext();
            rotationForce = (float)stream.ReceiveNext();
            sasStrength = (float)stream.ReceiveNext();
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
            accelerationCache = context.ReadValue<float>();
        }
    }

    public void OnBreak(InputAction.CallbackContext context)
    {
        if (AmOwningPilot())
        {
            breaksCache = context.ReadValue<float>();
        }
    }
}