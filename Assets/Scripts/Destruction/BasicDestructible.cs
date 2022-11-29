using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Destruct;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class BasicDestructible : MonoBehaviour, IDestructible
{
    MeshFilter mFilter;
    MeshCollider mCollider;

    void Start()
    {
        mFilter = GetComponent<MeshFilter>();
        mCollider = GetComponent<MeshCollider>();
    }

    void IDestructible.PreDestruct()
    {
        return;
    }

    void IDestructible.PostDestruct((SplitResult, List<GameObject>) destructionResults)
    {
        mFilter.mesh.SetVertices(destructionResults.Item1.vertices);
        mFilter.mesh.SetTriangles(destructionResults.Item1.triangles, 0);
        mFilter.mesh.RecalculateNormals();
        mFilter.mesh.RecalculateBounds();
        mCollider.convex = false;
        mCollider.sharedMesh = mFilter.mesh;
    }

    MeshFilter IDestructible.GetMeshFilter()
    {
        return mFilter;
    }
}