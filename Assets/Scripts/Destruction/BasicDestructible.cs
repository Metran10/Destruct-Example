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
        mFilter.mesh.MarkDynamic();
        mCollider = GetComponent<MeshCollider>();
    }

    void IDestructible.PreDestruct()
    {
        return;
    }

    void IDestructible.PostDestruct((SplitResult, List<SplitResult>) destructionResults)
    {
        var objects = Static.InstantiateObjectsFromSplitResults(destructionResults.Item2, transform.position, transform.rotation, GetComponent<MeshRenderer>().material);

        foreach (GameObject obj in objects)
        {
            obj.AddComponent<BasicDestructible>();
            var rb = obj.GetComponent<Rigidbody>();
            rb.AddForce(Random.onUnitSphere * 10f, ForceMode.VelocityChange);
        }
        if (destructionResults.Item1.triangles.Count == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            mFilter.mesh.SetVertices(destructionResults.Item1.vertices);
            mFilter.mesh.SetTriangles(destructionResults.Item1.triangles, 0);
            mFilter.mesh.RecalculateNormals();
            mFilter.mesh.RecalculateBounds();
            mCollider.convex = false;
            mCollider.sharedMesh = mFilter.mesh;
        }
    }

    MeshFilter IDestructible.GetMeshFilter()
    {
        return mFilter;
    }
}