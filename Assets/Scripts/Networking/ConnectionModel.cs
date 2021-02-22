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

    public void ChangeLocalPlayerTeamTo(Team newTeam)
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
        Dictionary<(Team, PlayerRole), int> distributionDict = new Dictionary<(Team, PlayerRole), int>();

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            var pair = (player.GetTeam(), player.GetRole());

            if (distributionDict.ContainsKey(pair))
                distributionDict[pair] += 1;
            else
                distributionDict[pair] = 1;
        }

        (Team, PlayerRole)[] checks = {(Team.Red, PlayerRole.Pilot), (Team.Red, PlayerRole.Gunner), (Team.Blue, PlayerRole.Pilot), (Team.Blue, PlayerRole.Gunner)};

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

public enum Team
{
    None = -1,
    Red = 0,
    Blue = 1
}

public static class Extensions
{
    public static Color ToColor(this Team team)
    {
        switch (team)
        {
            case Team.Red:
                return Color.red;
            case Team.Blue:
                return Color.blue;
            default:
                return Color.white;
        }
    }

    public static Team GetTeam(this Player p)
    {
        var o = p.CustomProperties["Team"];

        if (o == null)
            return Team.None;

        return (Team)o;
    }

    public static bool SetTeam(this Player p, Team teamId)
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