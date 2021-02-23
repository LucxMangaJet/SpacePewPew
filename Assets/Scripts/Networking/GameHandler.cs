using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using Photon.Realtime;

public class GameHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject playerCamPrefab;
    [SerializeField] GameObject spaceshipPrefab;

    [Header("LOCAL MODE DEBUG")]
    [SerializeField] Team localTeam;
    [SerializeField] PlayerRole localRole;

    Dictionary<Player, bool> playersReadyStates = new Dictionary<Player, bool>();
    public event System.Action AllReady;

    private void Awake()
    {
        ServiceLocator.SetGameHandler(this);
    }

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

    public void NotifyPlayerJoined(Player player)
    {
        playersReadyStates[player] = true;
        if (AllPlayersReady())
        {
            Debug.Log("All players ready!");
            AllReady?.Invoke();
        }
    }

    private bool AllPlayersReady()
    {
        foreach (var state in playersReadyStates)
        {
            if (!state.Value)
                return false;
        }
        return true;
    }

    private void Initialize()
    {
        if (PhotonNetwork.IsConnected)
        {
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                playersReadyStates.Add(player, false);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                InitializeGame();
            }

            InitializePlayer();
        }
        else
        {
            Debug.LogError("Not connected to photon network");
        }
    }

    private void InitializePlayer()
    {
        PhotonNetwork.Instantiate(ServiceLocator.PREFABS_PATH + playerCamPrefab.name, Vector3.zero, Quaternion.identity);
    }

    private void InitializeGame()
    {
        Debug.Log("Intitialize Game");
        var spaceship1 = PhotonNetwork.Instantiate(ServiceLocator.PREFABS_PATH + spaceshipPrefab.name, new Vector3(-5, 0, 0), Quaternion.identity).GetComponent<Spaceship>();
        spaceship1.Server_SetTeam(Team.Red);

        var spaceship2 = PhotonNetwork.Instantiate(ServiceLocator.PREFABS_PATH + spaceshipPrefab.name, new Vector3(5,0,0), Quaternion.identity).GetComponent<Spaceship>();
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
            PhotonNetwork.LocalPlayer.NickName = "Local Player";
            Initialize();
        }
    }
}
