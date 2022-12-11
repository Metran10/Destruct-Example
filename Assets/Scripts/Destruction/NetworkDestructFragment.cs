using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Destruct;

public class NetworkDestructFragment : NetworkBehaviour
{

    [Networked(OnChanged = nameof(OnIDSet))]
    public int ChildID { get; set; }


    [Networked]
    public TickTimer lifeTime { get; set; }

    [Networked]
    public TickTimer timeToInitialize { get; set; }

    [Networked(OnChanged = nameof(InitializeFragment))]
    public NetworkBool readyToInitialize { get; set; }


    NetworkDestructible destructParent;

    [SerializeField]
    public float life = 30.0f;

    [Networked]
    public NetworkId parentID { get; set; }


    public override void FixedUpdateNetwork()
    {
        if(destructParent == null)
        {
            if(parentID != null)
            {
                NetworkObject parent;
                Runner.TryFindObject(parentID, out parent);
                this.transform.parent = parent.gameObject.transform;

                destructParent = this.transform.parent.GetComponent<NetworkDestructible>();
            }
        }

        if (lifeTime.ExpiredOrNotRunning(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    public static void OnIDSet(Changed<NetworkDestructFragment> changed)
    {
        changed.Behaviour.readyToInitialize = true;
    }



    public override void Spawned()
    {
        lifeTime = TickTimer.CreateFromSeconds(Runner, life);
        timeToInitialize = TickTimer.CreateFromTicks(Runner, 10);

        if (destructParent == null)
        {
            if (parentID != null)
            {
                NetworkObject parent;
                Runner.TryFindObject(parentID, out parent);
                this.transform.parent = parent.gameObject.transform;

                destructParent = this.transform.parent.GetComponent<NetworkDestructible>();
            }
        }
    }

    public static void InitializeFragment(Changed<NetworkDestructFragment> changed)
    {
        bool isReadyNow = changed.Behaviour.readyToInitialize;
        changed.LoadOld();

        bool isReadyOld = changed.Behaviour.readyToInitialize;

        if(isReadyNow && !isReadyOld)
        {
            changed.Behaviour.InitializeFragmentMesh();
        }
    }

    public void InitializeFragmentMesh()
    {
        
        if (!Object.HasStateAuthority)
            return;
        
        SplitResult res = destructParent.childrenSplits[ChildID];

        MeshFilter mf = this.GetComponent<MeshFilter>();
        MeshRenderer mr = this.GetComponent<MeshRenderer>();
        MeshCollider mc = this.GetComponent<MeshCollider>();

        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.SetVertices(res.vertices);
        newMesh.SetTriangles(res.triangles, 0);
        newMesh.MarkDynamic();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        mf.mesh = newMesh;

        mr.material = destructParent.gameObject.GetComponent<MeshRenderer>().material;

        mc.convex = true;
        mc.sharedMesh = mf.mesh;

        GetComponent<NetworkTransform>().InterpolationTarget = this.transform;


    }

    public void initializeOnClient()
    {
        transform.localScale = Vector3.one;

        if (Object.HasStateAuthority)
            return;

        if (GetComponent<MeshCollider>() == null)
            gameObject.AddComponent<MeshCollider>();

        
        SplitResult res = destructParent.childrenSplits[ChildID];

        MeshFilter mf = this.GetComponent<MeshFilter>();
        MeshRenderer mr = this.GetComponent<MeshRenderer>();
        MeshCollider mc = this.GetComponent<MeshCollider>();

        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.SetVertices(res.vertices);
        newMesh.SetTriangles(res.triangles, 0);
        newMesh.MarkDynamic();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        mf.mesh = newMesh;

        mr.material = destructParent.gameObject.GetComponent<MeshRenderer>().material;

        mc.convex = true;
        mc.sharedMesh = mf.mesh;
    }


}
