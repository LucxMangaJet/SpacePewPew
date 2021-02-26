﻿using System.Collections;
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

    Dictionary<Team, int> scores = new Dictionary<Team, int>();
    public event System.Action ScoreChanged;

    public event System.Action<Spaceship> OnSpaceshipSpawned;


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

    public int GetScoreOf(Team t)
    {
        if (scores.ContainsKey(t))
        {
            return scores[t];
        }
        return 0;
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

        scores.Add(Team.Red, 0);
        scores.Add(Team.Blue, 0);
        ScoreChanged?.Invoke();
    }

    private void InitializeGame()
    {
        Debug.Log("Intitialize Game");
        Respawn(Team.Red);
        Respawn(Team.Blue);

    }

    private void OnDestroyedSpaceship(Spaceship obj)
    {
        Debug.Log(obj.Team + " destroyed");
        obj.Destroyed -= OnDestroyedSpaceship;

        if (obj.Team == Team.Blue)
            Server_AddScore(Team.Red);
        else
            Server_AddScore(Team.Blue);

        ScoreChanged?.Invoke();

        Respawn(obj.Team);
    }

    private void Respawn(Team team)
    {
        var spaceship = PhotonNetwork.Instantiate(ServiceLocator.PREFABS_PATH + spaceshipPrefab.name, new Vector3(10 * ((int)team), 0, 0), Quaternion.identity).GetComponent<Spaceship>();
        spaceship.Server_SetTeam(team);
        spaceship.Destroyed += OnDestroyedSpaceship;
        photonView.RPC(nameof(RPC_SpaceshipSpawned), RpcTarget.All, team);
    }

    [PunRPC]
    private void RPC_SpaceshipSpawned(Team t)
    {
        var sp = ServiceLocator.GetSpaceship(t);
        OnSpaceshipSpawned?.Invoke(sp);
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

    public void Server_AddScore(Team t)
    {
        photonView.RPC(nameof(RPC_AddScore), RpcTarget.AllBuffered, t);
    }

    [PunRPC]
    private void RPC_AddScore(Team t)
    {
        scores[t] += 1;
        ScoreChanged?.Invoke();
    }
}
