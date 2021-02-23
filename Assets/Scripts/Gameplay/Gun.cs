using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform bulletSpawnPoint;
    [SerializeField] float fireRate;

    Team team;
    Rigidbody2D originBody;
    bool isFiring = false;

    float lastShotStamp = -100;

    public void Fire()
    {
        string path = ServiceLocator.PREFABS_PATH + bulletPrefab.name;
        object[] spawnData = { team, originBody.velocity };

        PhotonNetwork.Instantiate(path, bulletSpawnPoint.position, transform.rotation, 0, spawnData);
        lastShotStamp = Time.time;
    }

    public void StartFiring(Rigidbody2D source, Team team)
    {
        this.team = team;
        this.originBody = source;
        isFiring = true;
    }

    public void StopFiring()
    {
        isFiring = false;
    }

    private void Update()
    {
        if (isFiring)
        {
            if(Time.time - lastShotStamp >= 1 / fireRate)
            {
                Fire();
            }
        }
    }
}
