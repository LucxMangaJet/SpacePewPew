using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunnerPosition : MonobehaviourPunPew
{
    [SerializeField] Spaceship spaceship;
    [SerializeField] Gun gun;

    Vector2 pointDirection;

    private void Update()
    {
        //I should be commanding
        if (AmOwningGunner())
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

    private bool AmOwningGunner()
    {
        return spaceship.Team == GetLocalTeam() && GetLocalPlayerRole() == PlayerRole.Gunner;
    }

    private void GunnerUpdate()
    {
        transform.up = -pointDirection;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (AmOwningGunner())
        {
            if (context.started)
            {
                gun.StartFiring(spaceship.Rigidbody, spaceship.Team);
            }
            else if (context.canceled)
            {
                gun.StopFiring();
            }
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (AmOwningGunner())
        {
            var value = context.ReadValue<Vector2>();
            pointDirection = value;
        }
    }

    public void OnMousePosition(InputAction.CallbackContext context)
    {
        if (AmOwningGunner())
        {
            var value = context.ReadValue<Vector2>();
            pointDirection = (value - new Vector2(Screen.width / 2, Screen.height / 2)).normalized;
        }
    }
}