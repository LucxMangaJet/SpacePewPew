using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunnerPosition : MonobehaviourPunPew
{
    [SerializeField] Spaceship spaceship;
    [SerializeField] Gun gun;
    Camera camera;

    protected override void Start()
    {
        camera = Camera.main;
    }

    private void Update()
    {
        //I should be commanding
        if (spaceship.Team == GetLocalTeam() && GetLocalPlayerRole() == PlayerRole.Gunner)
        {
            if (photonView.IsMine)
            {
                GunnerUpdate();
            }
            else
            {
                //dont have ownership even though I think I should, requesting it
                Debug.Log("Ownership request for gun control by " + PhotonNetwork.LocalPlayer.NickName);
                photonView.RequestOwnership();
            }
        }
    }

    private void GunnerUpdate()
    {
        Vector2 mousePos = Input.mousePosition - new Vector3(Screen.width/2, Screen.height/2);
        transform.up = -mousePos.normalized;

        if (Input.GetMouseButtonDown(0))
        {
            gun.StartFiring(spaceship.Rigidbody, spaceship.Team);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            gun.StopFiring();
        }
    }
}