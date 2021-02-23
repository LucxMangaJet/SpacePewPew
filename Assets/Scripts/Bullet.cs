using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Bullet : MonobehaviourPunPew, IPunInstantiateMagicCallback
{
    [SerializeField] float speed;
    [SerializeField] float lifetime;
    [SerializeField] new Rigidbody2D rigidbody2D;
    [SerializeField] SpriteRenderer spriteRenderer;
    Team team;

    private void Start()
    {
        if (photonView.IsMine)
        {
            Destroy(gameObject, lifetime);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        var data = photonView.InstantiationData;
        team = (Team)data[0];
        spriteRenderer.color = team.ToColor();
        //negative because gun faces down instead of up
        rigidbody2D.velocity = -transform.up * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (photonView.IsMine)
        {
            Destroy(gameObject);
        }
    }
}
