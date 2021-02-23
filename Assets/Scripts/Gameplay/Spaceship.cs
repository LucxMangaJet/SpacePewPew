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
    [SerializeField] float engineForce, rotationForce;
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
    private float rotationCompensation;

    private Vector3 directionalLightOffset;

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

        var emission = enginePS.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(10, 2000, Mathf.Abs(Mathf.Max(0, verticalCache)));

        emission = rcsLeftFront.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(horizontalCache, -rotationCompensation / 20)));

        emission = rcsRightFront.emission;
        emission.rateOverTimeMultiplier = (Mathf.Lerp(0, 300, Mathf.Max(-horizontalCache, rotationCompensation / 20)));
    }

    private void PilotUpdate()
    {
        //Spaceship sprite is upsidedown, thats why there are "-"
        rigidbody.AddTorque(rotationForce * -horizontalCache * Time.deltaTime);
        rigidbody.AddForce(transform.up * Mathf.Min(-0.05f, -verticalCache) * engineForce * Time.deltaTime);
        rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);

        if (Mathf.Abs(horizontalCache) < 0.1f)
        {
            float rot = -rigidbody.angularVelocity;
            rotationCompensation = rot;
        }
        else
        {
            rotationCompensation = 0;
        }

        rigidbody.AddTorque(rotationCompensation * rotationCompensationMultiplyer * Time.deltaTime);
        rigidbody.angularVelocity = Mathf.Clamp(rigidbody.angularVelocity, -maxRotationSpeed, maxRotationSpeed);
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
        }
        else
        {
            horizontalCache = (float)stream.ReceiveNext();
            verticalCache = (float)stream.ReceiveNext();
            rotationCompensation = (float)stream.ReceiveNext();
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