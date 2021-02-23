using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonobehaviourPunPew
{
    [SerializeField] Vector2 startRotationMinMax;
    [SerializeField] new Rigidbody2D rigidbody;


    protected override void OnAllReady()
    {
        if (photonView.IsMine)
        {
            float value = Mathf.Lerp(startRotationMinMax.x, startRotationMinMax.y, Random.value);
            rigidbody.AddTorque(value, ForceMode2D.Impulse);
        }
    }
}
