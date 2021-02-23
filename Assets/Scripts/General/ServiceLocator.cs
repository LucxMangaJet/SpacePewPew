using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public enum Location
{
    None,
    Cockpit,
    Weapon1
}

public class ServiceLocator : MonoBehaviour
{
    public const string PREFABS_PATH = "NetworkedObjects/";

    static ServiceLocator instance;

    Dictionary<LocationDescription, Transform> locations = new Dictionary<LocationDescription, Transform>();
    Dictionary<Team, Spaceship> spaceships = new Dictionary<Team, Spaceship>();
    GameHandler gameHandler;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.LogError("Multiple ServiceLocators found");
        }
    }

    public static bool IsValid()
    {
        return instance != null;
    }

    public static void SetGameHandler(GameHandler initializer)
    {
        AssertSingleton();
        instance.gameHandler = initializer;
    }

    public static GameHandler GetGameHandler()
    {
        AssertSingleton();
        return instance.gameHandler;
    }

    public static void SetLocation(Team team, Location location, Transform locationTarget)
    {
        AssertSingleton();
        instance.InternalSetLocation(team, location, locationTarget);
    }

    public static Vector3 GetLocation(Team team, Location location)
    {
        AssertSingleton();
        return instance.InternalGetLocation(team, location);
    }

    public static void AssertSingleton()
    {
        if (instance == null)
            throw new System.Exception("ServiceLocator is null");
    }

    private void InternalSetLocation(Team team, Location location, Transform locationTarget)
    {
        var desc = new LocationDescription(team, location);
        if (locations.ContainsKey(desc))
        {
            locations[desc] = locationTarget;
        }
        else
        {
            locations.Add(desc, locationTarget);
        }
    }

    private Vector3 InternalGetLocation(Team team, Location location)
    {
        var desc = new LocationDescription(team, location);
        if (locations.ContainsKey(desc))
        {
            var t = locations[desc];
            if (t == null)
                throw new System.Exception("ServiceLocator nullrefexception");

            return t.position;
        }
        else
        {
            throw new System.Exception("ServiceLocator target not defined: (" + team + " " + location + ")");
        }
    }

    public static void SetSpaceship(Team team, Spaceship spaceship)
    {
        AssertSingleton();
        instance.InteralSetSpaceship(team, spaceship);
    }

    public void InteralSetSpaceship(Team t, Spaceship s)
    {
        if (spaceships.ContainsKey(t))
            spaceships[t] = s;
        else
            spaceships.Add(t, s);
    }

    public static Spaceship GetSpaceship(Team t)
    {
        AssertSingleton();
        return instance.InternalGetSpaceship(t);
    }

    public Spaceship InternalGetSpaceship(Team t)
    {
        if (spaceships.ContainsKey(t))
        {
            return spaceships[t];
        }

        throw new System.Exception("Undefined spaceship for team " + t);
    }

    public static Player GetFirstPlayerMatching(Team t, PlayerRole role)
    {
       var r = PhotonNetwork.CurrentRoom.Players.Values.FirstOrDefault((x) => x.GetTeam() == t && x.GetRole() == role);
       return r;
    }

    private struct LocationDescription
    {
        public Team Team;
        public Location Location;

        public LocationDescription(Team team, Location location)
        {
            Team = team;
            Location = location;
        }
    }
}
