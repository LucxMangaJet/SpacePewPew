using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class GameInitializer : MonoBehaviourPunCallbacks
{
    const string PREFABS_PATH = "NetworkedObjects/";

    [SerializeField] GameObject playerCamPrefab;
    [SerializeField] GameObject spaceshipPrefab;

    [Header("LOCAL MODE DEBUG")]
    [SerializeField] Team localTeam;
    [SerializeField] PlayerRole localRole;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = true;
        }
        else
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                InitializeGame();
            }

            InitializePlayer();
        }
        else
        {
            Debug.Log("Not connected to photon network");
        }
    }

    private void InitializePlayer()
    {
        PhotonNetwork.Instantiate(PREFABS_PATH + playerCamPrefab.name, Vector3.zero, Quaternion.identity);
    }

    private void InitializeGame()
    {
        Debug.Log("Intitialize Game");
        var spaceship1 = PhotonNetwork.Instantiate(PREFABS_PATH + spaceshipPrefab.name, new Vector3(-5, 0, 0), Quaternion.identity).GetComponent<Spaceship>();
        spaceship1.Server_SetTeam(Team.Red);

        var spaceship2 = PhotonNetwork.Instantiate(PREFABS_PATH + spaceshipPrefab.name, new Vector3(5,0,0), Quaternion.identity).GetComponent<Spaceship>();
        spaceship2.Server_SetTeam(Team.Blue);
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.CreateRoom("OfflineMode");
        }
    }

    public override void OnCreatedRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.LocalPlayer.SetTeam(localTeam);
            PhotonNetwork.LocalPlayer.SetRole(localRole);

            Initialize();
        }
    }
}
