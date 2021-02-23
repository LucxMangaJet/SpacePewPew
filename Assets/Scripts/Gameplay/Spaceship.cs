using Photon.Compression;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

//Maybe write own client side prediction: https://www.kinematicsoup.com/news/2017/5/30/multiplayerprediction

public interface IDamagable
{
    void TakeDamage(Bullet bullet, float amount);
}

public class Spaceship : MonobehaviourPunPew, IDamagable
{
    [SerializeField] float engineForce, rotationForce;
    [SerializeField] float maxHealth;

    [Header("Components")]
    [SerializeField] SpriteRenderer teamColorsRenderer;
    [SerializeField] Transform cockpitCamTarget, weapon1CamTarget;
    [SerializeField] new Rigidbody2D rigidbody;
    [SerializeField] new Light2D light;

    private Team owningTeam;
    private float health;

    public System.Action<Spaceship> HealthChanged;

    public Team Team { get => owningTeam; }

    public float Health { get => health; }
    public float MaxHealth { get => maxHealth; }

    protected override void OnAllReady()
    {
        health = maxHealth;
    }

    private void Update()
    {
        //I should be commanding
        if (owningTeam == GetLocalTeam() && GetLocalPlayerRole() == PlayerRole.Pilot)
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
    }

    private void PilotUpdate()
    {
        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");

        //Spaceship sprite is upsidedown, thats why there are "-"
        rigidbody.AddTorque(rotationForce * -horizontal * Time.deltaTime);
        rigidbody.AddForce(transform.up * -vertical * engineForce * Time.deltaTime);
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
}