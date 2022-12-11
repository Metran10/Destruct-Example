using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Missile : NetworkBehaviour
{
    private bool hit = false;
    [Networked]
    private TickTimer life { get; set; }

    [SerializeField]
    public int granulationStrenght = 2;
    
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
            hit = true;

            if(collision.gameObject.GetComponent<IDestructible>() != null)
            {
                NetworkDestructible ob = collision.gameObject.GetComponent<NetworkDestructible>();

                if (!ob.isReadyToDestruct)
                {
                    int explosionSeed = Random.Range(0, 10000);
                    ob.RPC_SendInfoAboutDestruction(this.transform.position, explosionSeed, granulationStrenght);
                }
                Runner.Despawn(Object);
            }
            Runner.Despawn(Object);
        }
    }


}
