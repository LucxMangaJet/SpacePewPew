using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonobehaviourPunPew
{
    [SerializeField] Vector2 startRotationMinMax;
    [SerializeField] new Rigidbody2D rigidbody;
    [SerializeField] Transform lightTransform;
    [SerializeField] Vector3 lightOffset;


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
}
