using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public static void SetLocation(Team team, Location location, Transform locationTarget)
    {
        if (instance != null)
            instance.InternalSetLocation(team, location, locationTarget);
    }

    public static Vector3 GetLocation(Team team, Location location)
    {
        if (instance == null)
            throw new System.Exception("ServiceLocator is null");

        return instance.InternalGetLocation(team, location);
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
