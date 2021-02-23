using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonobehaviourPunPew
{
    [SerializeField] Vector3 offset;
    [SerializeField] Behaviour[] toDisableOnOthers;

    Location viewTarget;
    Team team;


    private void Start()
    {
        UpdateTargetsBasedOnRole();

        if (!photonView.IsMine)
        {
            foreach (var c in toDisableOnOthers)
                c.enabled = false;
        }
        
    }

    private void UpdateTargetsBasedOnRole()
    {
        var player = PhotonNetwork.LocalPlayer;

        team = player.GetTeam();

        switch (player.GetRole())
        {
            case PlayerRole.Pilot:
                viewTarget = Location.Cockpit;
                break;
            case PlayerRole.Gunner:
                viewTarget = Location.Weapon1;
                break;

            default:
                viewTarget = Location.None;
                break;
        }
    }

    private void Update()
    {
        if (viewTarget == Location.None)
        {
            //add spectator control
        }
        else
        {
            var target = ServiceLocator.GetLocation(team, viewTarget);
            transform.position = target + offset;
        }
    }
}
