using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Missile : NetworkBehaviour
{
    private bool hit = false;
    [Networked]
    private TickTimer life { get; set; }

    
    public void Init(Vector3 aimVector)
    {
        life = TickTimer.CreateFromSeconds(Runner, 6f);
        GetComponent<Rigidbody>().velocity = aimVector * 3;
    }


    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!hit)
        {
            //Debug.Log("HIT");
            hit = true;
        }
        
    }


}
