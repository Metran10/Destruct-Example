using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Missile : NetworkBehaviour
{
    private bool hit = false;
    [Networked]
    private TickTimer life { get; set; }

    public float explosionradius = 3;

    [SerializeField]
    public int granulation = 6;
    
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



            if(collision.gameObject.GetComponent<IDestructible>() != null)
            {
                NetworkDestructible ob = collision.gameObject.GetComponent<NetworkDestructible>();
                //ob.hitPosition = this.transform.position;
                //ob.destructionRadius = explosionradius;

                if (!ob.isReadyToDestruct)
                {
                    Debug.Log("changed bool");
                    Debug.Log("SENT RPC");
                    //ob.RPC_ExecuteDestructOnClient();
                    //ob.isReadyToDestruct = true;

                    int explosionSeed = Random.Range(0, 10000);
                    ob.RPC_SendInfoAboutDestruction(this.transform.position, explosionSeed, granulation);


                    
                }


                //ob.OnDestruction(explosionradius);

                

                Debug.Log("Hit destructible network object");

                Runner.Despawn(Object);


            }

        }
        
    }


}
