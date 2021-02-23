using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Maybe write own client side prediction: https://www.kinematicsoup.com/news/2017/5/30/multiplayerprediction

public class Spaceship : MonobehaviourPunPew
{
    [SerializeField] float engineForce, rotationForce, lightDistance;

    [SerializeField] SpriteRenderer teamColorsRenderer;
    [SerializeField] Transform cockpitCamTarget, weapon1CamTarget, lightSource;
    [SerializeField] new Rigidbody2D rigidbody;

    private Team owningTeam;

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

        lightSource.transform.position = transform.position + (Vector3.right + Vector3.up) * lightDistance;
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

        ServiceLocator.SetLocation(team, Location.Cockpit, cockpitCamTarget);
        ServiceLocator.SetLocation(team, Location.Weapon1, weapon1CamTarget);
    }

}