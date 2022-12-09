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


    public List<NetworkObject> childrenObjects;

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
        Debug.Log("Started post destruction");



        Debug.Log($"GRANULARITY: {granularity}");

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

                        //o.AddBehaviour<NetworkDestructible>();
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
                childrenSplits = destructionResults.Item2;

                //client side
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
        
        Debug.Log($"Destroy with gran: {granularity} * {explosionStrenght}");

        var tmpTime = Time.realtimeSinceStartup;

        Static.Destruct(this, transform.InverseTransformPoint(hitPosition), float.PositiveInfinity, granularity * explosionStrenght);

        //Debug.Log($"Destruct executed in {Time.realtimeSinceStartup - tmpTime}");
        //Debug.Log($"Object splitted into {childrenSplits.Count}");
        if(statText != null)
            statText.text = $"Object destoyed in \n{Time.realtimeSinceStartup - tmpTime}\n {childrenSplits.Count} fragments\n strenght: {granularity * explosionStrenght}";
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
        Debug.Log($"Server received info about explosion pos:{explosionPosition}, seed {explosionSeed}, explosion strenght: {explosionStrenght}");
        //seed = explosionSeed;
        hitPosition = explosionPosition;
        this.explosionStrenght = explosionStrenght;
        seed = explosionSeed;
        //Random.InitState(seed);
        isReadyToDestruct = true;

        
        
        //granularity = granulation;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.Proxies)]
    public void RPC_destructOnClient(int explosionStrenght, int destructSeed)
    {
        Debug.Log("RPC request destruct on clients");
        this.explosionStrenght = explosionStrenght;
        seed = destructSeed;
        //Random.InitState(seed);

        DestroyObject();

    }


    

}
