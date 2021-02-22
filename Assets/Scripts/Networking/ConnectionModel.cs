using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using Photon.Realtime;

public class ConnectionModel : MonoBehaviourPunCallbacks
{
    List<RoomInfo> availableRooms;

    public event System.Action<string> ConnectionError;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void ConnectToServer()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRandom()
    {
        PhotonNetwork.CreateRoom("LucaTest");
    }

    internal void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void JoinRoom(string name)
    {
        PhotonNetwork.JoinRoom(name);
    }

    public void RenameLocalPlayerTo(string newName)
    {
        PhotonNetwork.LocalPlayer.NickName = newName;
        PlayerPrefs.SetString("Nickname", newName);
    }

    public void ChangeLocalPlayerRoleTo(PlayerRole newRole)
    {
        PhotonNetwork.LocalPlayer.SetRole(newRole);
    }

    public void ChangeLocalPlayerTeamTo(int newTeam)
    {
        PhotonNetwork.LocalPlayer.SetTeam(newTeam);
    }

    internal void JoinDefaultLobby()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        availableRooms = roomList;
    }

    public List<RoomInfo> GetAllRooms()
    {
        return availableRooms;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ConnectionError?.Invoke("Create Room Failed: " + message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        ConnectionError?.Invoke("Join Random Room Failed: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ConnectionError?.Invoke("Join Room Failed: " + message);
    }

    public override void OnJoinedRoom()
    {
        var name = PlayerPrefs.GetString("Nickname", "New Player");
        PhotonNetwork.LocalPlayer.NickName = name;
    }

    internal void StartGame()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    public bool IsRoomWellDistributed(out string failReason)
    {
        Dictionary<(int, PlayerRole), int> distributionDict = new Dictionary<(int, PlayerRole), int>();

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            var pair = (player.GetTeam(), player.GetRole());

            if (distributionDict.ContainsKey(pair))
                distributionDict[pair] += 1;
            else
                distributionDict[pair] = 1;
        }

        (int, PlayerRole)[] checks = {(0, PlayerRole.Pilot), (0, PlayerRole.Gunner), (1, PlayerRole.Pilot), (1, PlayerRole.Gunner)};

        foreach (var check in checks)
        {
            var count = distributionDict.ContainsKey(check) ?  distributionDict[check] : 0;

            if(count == 0)
            {
                failReason = "No " + check.Item2 + " in Team " + check.Item1;
                return false;
            }
            else if(count > 1)
            {
                failReason = "Too many " + check.Item2 +  " in Team " + check.Item1 + "Currently (" + count + ")";
                return false;
            }
        }

        failReason = "";
        return true;
    }
}


public enum PlayerRole
{
    Spectator,
    Pilot,
    Gunner
}

public static class PhotonExtensions
{
    public static int GetTeam(this Player p)
    {
        var o = p.CustomProperties["Team"];

        if (o == null)
            return -1;

        return (int)o;
    }

    public static bool SetTeam(this Player p, int teamId)
    {
        if (p.IsLocal)
        {
            var prop = p.CustomProperties;
            prop["Team"] = teamId;
            p.SetCustomProperties(prop);
            return true;
        }
        else
        {
            Debug.LogWarning("Trying to set team of other player.");
            return false;
        }
    }

    public static PlayerRole GetRole(this Player p)
    {
        var o = p.CustomProperties["Role"];

        if (o == null)
            return PlayerRole.Spectator;

        return (PlayerRole)o;
    }

    public static bool SetRole(this Player p, PlayerRole newRole)
    {
        if (p.IsLocal)
        {
            var prop = p.CustomProperties;
            prop["Role"] = newRole;
            p.SetCustomProperties(prop);
            return true;
        }
        else
        {
            Debug.LogWarning("Trying to set role of other player.");
            return false;
        }
    }
}