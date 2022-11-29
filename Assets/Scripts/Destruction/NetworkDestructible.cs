using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Destruct;

public class NetworkDestructible : NetworkBehaviour, IDestructible
{   
    [Networked(OnChanged = nameof(OnObjectDestruction))]
    public bool isReadyToDestruct { get; set; }


    public Vector3 hitPosition;


    MeshFilter mFilter;
    MeshCollider mCollider;

    void Start()
    {
        mFilter = GetComponent<MeshFilter>();
        mCollider = GetComponent<MeshCollider>();
    }

    public MeshFilter GetMeshFilter()
    {
        return mFilter;
    }

    public void PreDestruct()
    {
        Debug.Log($"Called pre destruct");

        Debug.Log($"Object position in world: {transform.position}");
        Debug.Log($"Hit position: {hitPosition}");

        //Static.Destruct(this, hitPosition, 10);

    }

    public void PostDestruct((SplitResult, List<GameObject>) destructionResults)
    {
        Debug.Log("Started post destruction");
        var currentOB = destructionResults.Item1;
        mFilter.mesh.SetVertices(currentOB.vertices);
        mFilter.mesh.SetTriangles(currentOB.triangles, 0);
        mFilter.mesh.RecalculateNormals();
        mFilter.mesh.RecalculateBounds();
        mCollider.convex = false;
        mCollider.sharedMesh = mFilter.mesh;

        Debug.Log("called postDestruct");
    }

    public override void FixedUpdateNetwork()
    {
        
    }

    public void OnDestruction()
    {
        Debug.Log("called on destruction");
        Debug.Log($"isReady {isReadyToDestruct}");
        //Static.Destruct(this, transform.InverseTransformPoint(hitPosition), 2);
        isReadyToDestruct = true;
    }

    public static void OnObjectDestruction(Changed<NetworkDestructible> changed)
    {
        Debug.Log("On object destruction");
        bool isReadyToDestructCurr = changed.Behaviour.isReadyToDestruct;

        changed.LoadOld();
        bool isReadyToDestructOld = changed.Behaviour.isReadyToDestruct;

        if(isReadyToDestructCurr && !isReadyToDestructOld)
        {
            changed.Behaviour.DestroyObject();
        }


    }

    public void DestroyObject()
    {
        Debug.Log("destroying");
        Static.Destruct(this, transform.InverseTransformPoint(hitPosition), 1);
        isReadyToDestruct = false;
        Debug.Log($"isReady {isReadyToDestruct}");
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
