using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonobehaviourPunPew, IDamagable
{
    [SerializeField] Vector2 startRotationMinMax;
    [SerializeField] Vector2 explosionForceMinMax;
    [SerializeField] new Rigidbody2D rigidbody;
    [SerializeField] Transform lightTransform;
    [SerializeField] Vector3 lightOffset;
    [SerializeField] float hp = 40, maxHp = 40;


    protected override void OnAllReady()
    {
        if (photonView.IsMine)
        {
            float value = Mathf.Lerp(startRotationMinMax.x, startRotationMinMax.y, Random.value);
            rigidbody.AddTorque(value, ForceMode2D.Impulse);
        }
    }

    protected override void Start()
    {
        base.Start();

        if (photonView.IsMine)
        {
            hp = maxHp * transform.localScale.magnitude;
        }
    }

    private void Update()
    {
        lightTransform.position = transform.position + lightOffset;
    }

    public void TakeDamage(Bullet bullet, float amount)
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(bullet.gameObject);

            hp -= amount;

            if (hp <= 0)
            {
                transform.localScale = transform.localScale * 0.66f;
                float scaleMagnitude = transform.localScale.magnitude;

                if(scaleMagnitude < 0.2f)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                else
                {
                    hp = maxHp * scaleMagnitude;
                    Vector3 dir = transform.position - bullet.transform.position;
                    Vector3 forceDir = Quaternion.Euler(0, 0, 90) * dir.normalized;


                    string path = ServiceLocator.PREFABS_PATH + "Asteroid";
                    GameObject newAsteroid = PhotonNetwork.Instantiate(path, transform.position - forceDir * scaleMagnitude, transform.rotation);
                    newAsteroid.transform.localScale = transform.localScale;
                    transform.position += forceDir * scaleMagnitude;

                    float force = Random.Range(explosionForceMinMax.x, explosionForceMinMax.y);
                    rigidbody.AddForce(forceDir * force, ForceMode2D.Impulse);
                    var otherRigidbody = newAsteroid.GetComponent<Rigidbody2D>();
                    otherRigidbody.AddForce(-forceDir * force, ForceMode2D.Impulse);
                    otherRigidbody.AddForce(-forceDir * force, ForceMode2D.Impulse);
                }




            }
        }
    }
}
