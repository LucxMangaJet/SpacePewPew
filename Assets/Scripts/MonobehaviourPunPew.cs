using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonobehaviourPunPew : MonoBehaviourPun
{


    protected PlayerRole GetLocalPlayerRole()
    {
        return PhotonNetwork.LocalPlayer.GetRole();
    }

    protected Team GetLocalTeam()
    {
        return PhotonNetwork.LocalPlayer.GetTeam();
    }

}
