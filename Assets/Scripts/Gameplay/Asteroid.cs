using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonobehaviourPunPew, IDamagable
{
    [SerializeField] Vector2 startRotationMinMax;
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
                hp = maxHp;
                transform.localScale = transform.localScale * 0.66f;
                transform.position -= (Vector3.left * 0.25f);
                string path = ServiceLocator.PREFABS_PATH + "Asteroid";
                GameObject newAsteroid = PhotonNetwork.Instantiate(path, transform.position + Vector3.right * 0.5f, transform.rotation);
                newAsteroid.transform.localScale = transform.localScale;
            }
        }
    }
}
