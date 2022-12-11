using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Destruct;
using TMPro;

public class NetworkDestructible : NetworkBehaviour, IDestructible
{   
    [Networked(OnChanged = nameof(OnObjectDestruction))]
    public NetworkBool isReadyToDestruct { get; set; }

    [Networked]
    public NetworkBool isDestructed { get; set; }

    [SerializeField]
    public TextMeshProUGUI statText;

    public NetworkObject destructElemPF;

    public int granularity = 6;


    [Networked]
    public TickTimer destructionCD { get; set; }

    [Networked]
    public Vector3 hitPosition { get; set; }
    [Networked]
    public float destructionRadius { get; set; }


    MeshFilter mFilter;
    MeshCollider mCollider;


    [Networked(OnChanged = nameof(OnSeedChange))]
    public int seed { get; set; }

    public int choosenSeed = 1000;

    public int explosionStrenght = 1;

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

    public override void Spawned()
    {   
        
    }

    public MeshFilter GetMeshFilter()
    {
        return mFilter;
    }

    public void PreDestruct()
    {
        Random.InitState(seed);
        destructionCD = TickTimer.CreateFromSeconds(Runner, 1f);
    }

    public void PostDestruct((SplitResult, List<SplitResult>) destructionResults)
    {
        transform.localScale = Vector3.one;

        if (Object.HasStateAuthority)
        {
            if(!isLocallyDest)
            {
                //server side
                childrenSplits = destructionResults.Item2;

                var currentOB = destructionResults.Item1;

                //clearing basic obj
                mFilter.mesh = null;
                mCollider.enabled = false;

                for (int i = 0; i < destructionResults.Item2.Count; i++)
                {
                    if (Object.HasStateAuthority)
                    {
                        NetworkObject o = Runner.Spawn(destructElemPF, transform.position, transform.rotation, null,
                             (runner, o) => { PrepareObject(o, this, i); });

                        o.GetComponent<NetworkDestructFragment>().ChildID = i;
                        o.GetComponent<NetworkDestructFragment>().parentID = Object.Id;

                        o.gameObject.transform.parent = this.gameObject.transform;

                    }
                }

                RPC_destructOnClient(explosionStrenght, seed);
                isLocallyDest = true;
            }
        }
        else
        {
            if (!isLocallyDest)
            {
                //client side
                childrenSplits = destructionResults.Item2;
                
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
        }
    }

    public void OnDestruction(float explosionRadius)
    {

        destructionRadius = explosionRadius;
        if (!isReadyToDestruct)
        {
            isReadyToDestruct = true;
        }
    }

    public static void OnObjectDestruction(Changed<NetworkDestructible> changed)
    {

        bool isReadyToDestructCurr = changed.Behaviour.isReadyToDestruct;

        changed.LoadOld();
        bool isReadyToDestructOld = changed.Behaviour.isReadyToDestruct;

        if (isReadyToDestructCurr && !isReadyToDestructOld)
        {
            changed.Behaviour.DestroyObject();

        }
    }

    public void DestroyObject()
    {
        var tmpTime = Time.realtimeSinceStartup;

        Static.Destruct(this, transform.InverseTransformPoint(hitPosition), float.PositiveInfinity, granularity * explosionStrenght);

        if(statText != null)
            statText.text = $"Object destoyed in \n{Time.realtimeSinceStartup - tmpTime}\n {childrenSplits.Count} fragments\n strenght: {granularity * explosionStrenght}";
    }



    public static void OnSeedChange(Changed<NetworkDestructible> changed)
    {
        
        int currentSeed = changed.Behaviour.seed;

        Random.InitState(currentSeed);
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
        MeshRenderer mr = ob.GetComponent<MeshRenderer>();
        mf.mesh.SetVertices(split.vertices);
        mf.mesh.SetTriangles(split.triangles, 0);
        mf.mesh.Optimize();
        mf.mesh.RecalculateNormals();
        mf.mesh.RecalculateBounds();

        mr.material = parent.GetComponent<MeshRenderer>().material;

        mc.convex = true;
    }

    


    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPC_SendInfoAboutDestruction(Vector3 explosionPosition, int explosionSeed, int explosionStrenght)
    {
        hitPosition = explosionPosition;
        this.explosionStrenght = explosionStrenght;
        seed = explosionSeed;
        Random.InitState(seed);
        isReadyToDestruct = true;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.Proxies)]
    public void RPC_destructOnClient(int explosionStrenght, int destructSeed)
    {
        this.explosionStrenght = explosionStrenght;
        seed = destructSeed;

        Random.InitState(seed);

        DestroyObject();
    }


    

}
