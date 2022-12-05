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


    public float life = 1000f;//60.0f;

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

        //if (timeToInitialize.Expired(Runner))
        //{
        //    readyToInitialize = true;
        //}

    }

    public static void OnIDSet(Changed<NetworkDestructFragment> changed)
    {
        //Debug.Log($"Current id: {changed.Behaviour.ChildID}");

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
        //this.gameObject.AddComponent<MeshCollider>();
        if (!Object.HasStateAuthority)
        {
            return;
            //Debug.Log("KLIENT");
            //Debug.Log($"parentID: {parentID}, wielkosc listy {destructParent.childrenSplits.Count}");
        }

        //############ no tutaj cos nie dziala przez losowosc

        SplitResult res = destructParent.childrenSplits[ChildID];
        //Debug.Log("Inicjalizacja meshu");
        //Debug.Log($"ID: {ChildID}|  vertices: {res.vertices.Count} | triangles: {res.triangles.Count}");
        


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

        //mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        //mf.mesh.SetVertices(res.vertices);
        //mf.mesh.SetTriangles(res.triangles, 0);
        //mf.mesh.MarkDynamic();
        //mf.mesh.RecalculateNormals();
        //mf.mesh.RecalculateBounds();

        mr.material = destructParent.gameObject.GetComponent<MeshRenderer>().material;

        mc.convex = true;
        mc.sharedMesh = mf.mesh;

        GetComponent<NetworkTransform>().InterpolationTarget = this.transform;


    }

    public void initializeOnClient()
    {
        Debug.Log("Initializing fragment mesh on CLIENT");

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
