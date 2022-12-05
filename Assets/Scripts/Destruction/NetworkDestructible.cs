using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Destruct;

public class NetworkDestructible : NetworkBehaviour, IDestructible
{   
    [Networked(OnChanged = nameof(OnObjectDestruction))]
    public NetworkBool isReadyToDestruct { get; set; }

    [Networked]
    public NetworkBool isDestructed { get; set; }




    public NetworkObject destructElemPF;
    public int granularity = 3;


    [Networked]
    public TickTimer destructionCD { get; set; }

    [Networked]
    public Vector3 hitPosition { get; set; }
    [Networked]
    public float destructionRadius { get; set; }


    public List<NetworkObject> childrenObjects;

    MeshFilter mFilter;
    MeshCollider mCollider;


    [Networked(OnChanged = nameof(OnSeedChange))]
    public int seed { get; set; }

    public int choosenSeed = 1000;



    public List<SplitResult> childrenSplits;

    private bool isLocallyDest = false;


    void Start()
    {
        mFilter = GetComponent<MeshFilter>();
        mCollider = GetComponent<MeshCollider>();
        mCollider.convex = true;

        Random.InitState(choosenSeed);

        childrenSplits = new List<SplitResult>();
    }

    public MeshFilter GetMeshFilter()
    {
        return mFilter;
    }

    public void PreDestruct()
    {
        Random.InitState(choosenSeed);
        destructionCD = TickTimer.CreateFromSeconds(Runner, 1f);
    }

    public void PostDestruct((SplitResult, List<SplitResult>) destructionResults)
    {
        transform.localScale = Vector3.one;
        Debug.Log("Started post destruction");
        

        


        if (Object.HasStateAuthority)
        {
            if(!isLocallyDest)
            {
                //strona serwerowa
                childrenSplits = destructionResults.Item2;

                Debug.Log("Post destruct on Server");
                Debug.Log($"Children splits on SERVER: {childrenSplits.Count}");
                Debug.Log($"{destructionResults.Item2[0]}");


                var currentOB = destructionResults.Item1;

                //clearing basic obj
                mFilter.mesh = null;
                mCollider.enabled = false;

                for (int i = 0; i < destructionResults.Item2.Count; i++)
                {
                    //Debug.Log("Spawning fragments");

                    if (Object.HasStateAuthority)
                    {
                        NetworkObject o = Runner.Spawn(destructElemPF, transform.position, transform.rotation, null,
                             (runner, o) => { PrepareObject(o, this, i); });
                        //childrenObjects.Add(o);
                        o.GetComponent<NetworkDestructFragment>().ChildID = i;
                        o.GetComponent<NetworkDestructFragment>().parentID = Object.Id;

                        o.gameObject.transform.parent = this.gameObject.transform;
                    }
                }

                RPC_destructOnClient();
                isLocallyDest = true;
            }
        }
        else
        {
            if (!isLocallyDest)
            {
                childrenSplits = destructionResults.Item2;

                //strona kliencka
                Debug.Log("Post destruct on CLIENT");

                Debug.Log($"Split count on CLIENT: {childrenSplits.Count}");
                Debug.Log($"{destructionResults.Item2[0]}");
                Debug.Log($"Object has {transform.childCount} children");

                //clear basic object
                mFilter.mesh = null;
                mCollider.enabled = false;

                //get meshes for client's fragments
                foreach (Transform child in transform)
                {
                    child.GetComponent<NetworkDestructFragment>().initializeOnClient();
                }
                isLocallyDest = true;
            }

        }

        

    }

    public override void FixedUpdateNetwork()
    {
        if (destructionCD.ExpiredOrNotRunning(Runner))
        {
            isReadyToDestruct = false;
            //Debug.Log("Changing bool to false");
        }
    }

    public void OnDestruction(float explosionRadius)
    {
        //Debug.Log("called on destruction");
        //Debug.Log($"isReady {isReadyToDestruct}");

        if (!Object.HasStateAuthority)
        {
            Debug.Log("KLIENT OnDestruction");
        }

        destructionRadius = explosionRadius;
        if (!isReadyToDestruct)
        {
            //Debug.Log("Changing bool to true");
            isReadyToDestruct = true;
        }

        
    }

    public static void OnObjectDestruction(Changed<NetworkDestructible> changed)
    {
        //Debug.Log("ON CHANGE isreadytodestruct");


        if (!changed.Behaviour.HasStateAuthority)
        {
            Debug.Log("KLIENT On ObjectDestruction");
        }
        //Debug.Log("OnObjectDestruction");

        bool isReadyToDestructCurr = changed.Behaviour.isReadyToDestruct;

        changed.LoadOld();
        bool isReadyToDestructOld = changed.Behaviour.isReadyToDestruct;

        if (isReadyToDestructCurr && !isReadyToDestructOld)
        {
            Debug.Log("Destrukcja");
            //#########################################################################################################
            changed.Behaviour.DestroyObject();

        }
        else if (!isReadyToDestructCurr && isReadyToDestructOld)
        {
            //Debug.Log("A mog³o zabic");
        }
    }

    public void DestroyObject()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.Log("KLIENT Destroy ");
        }
        else
        {
            Debug.Log("Server destroy");
        }

        Static.Destruct(this, transform.InverseTransformPoint(hitPosition), float.PositiveInfinity, granularity);

    }



    public static void OnSeedChange(Changed<NetworkDestructible> changed)
    {
        
        int currentSeed = changed.Behaviour.seed;

        Random.InitState(currentSeed);

        Debug.Log($"SEED CHANGE to {changed.Behaviour.seed}");

    }

    public static void PrepareObject(NetworkObject ob, NetworkDestructible parent ,int meshID)
    {
        ob.GetComponent<NetworkDestructFragment>().ChildID = meshID;
        ob.GetComponent<NetworkDestructFragment>().parentID = parent.Object.Id;

        SplitResult split = parent.childrenSplits[meshID];
        ob.gameObject.AddComponent<Rigidbody>();
        ob.gameObject.AddComponent<MeshCollider>();

        MeshFilter mf = ob.GetComponent<MeshFilter>();
        MeshCollider mc = ob.GetComponent<MeshCollider>();
        mf.mesh.SetVertices(split.vertices);
        mf.mesh.SetTriangles(split.triangles, 0);
        mf.mesh.Optimize();
        mf.mesh.RecalculateNormals();
        mf.mesh.RecalculateBounds();

        mc.convex = true;



        //ob.GetComponent<Rigidbody>().isKinematic = true;
        //Debug.Log("Added components");
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ExecuteDestructOnClient()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.Log("Client received rpc from server");
        }
        else
        {
            Debug.Log("Server received rpc from server");
        }


    }


    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPC_SendInfoAboutDestruction(Vector3 explosionPosition, int explosionSeed)
    {
        Debug.Log($"Server received info about explosion pos:{explosionPosition}, seed {explosionSeed}");
        //seed = explosionSeed;
        isReadyToDestruct = true;
        hitPosition = explosionPosition;

    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.Proxies)]
    public void RPC_destructOnClient()
    {
        Debug.Log("RPC request destruct on clients");

        DestroyObject();

    }


    

}
