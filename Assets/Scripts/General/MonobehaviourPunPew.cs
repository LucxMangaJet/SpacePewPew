using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonobehaviourPunPew : MonoBehaviourPun
{
    protected virtual void Start()
    {
        ServiceLocator.GetGameHandler().AllReady += OnAllReady;
    }

    protected virtual void OnAllReady()
    {

    }

    protected virtual void OnDestroy()
    {
        if (ServiceLocator.IsValid())
            ServiceLocator.GetGameHandler().AllReady -= OnAllReady;
    }

    protected PlayerRole GetLocalPlayerRole()
    {
        return PhotonNetwork.LocalPlayer.GetRole();
    }

    protected Team GetLocalTeam()
    {
        return PhotonNetwork.LocalPlayer.GetTeam();
    }

}
