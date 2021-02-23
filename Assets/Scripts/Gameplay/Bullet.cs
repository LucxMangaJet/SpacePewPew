using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Bullet : MonobehaviourPunPew, IPunInstantiateMagicCallback
{
    [SerializeField] float speed;
    [SerializeField] float lifetime;
    [SerializeField] float damage;
    [SerializeField] new Rigidbody2D rigidbody2D;
    [SerializeField] SpriteRenderer spriteRenderer;
    Team team;

    public Team Team { get => team; }

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
        Vector3 baseVel = (Vector2)data[1];
        rigidbody2D.velocity = baseVel - transform.up * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if(collision.TryGetComponent(out IDamagable damagable))
            {
                damagable.TakeDamage(this, damage);
            }
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
