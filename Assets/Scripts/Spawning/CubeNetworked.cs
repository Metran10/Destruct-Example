using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CubeNetworked : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnInitializeCube))]
    public bool isInitialized { get; set; }

    MeshFilter meshFilter;

    [Networked]
    public TickTimer lifetime { get; set; }


    public override void FixedUpdateNetwork()
    {
        if (lifetime.ExpiredOrNotRunning(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    public override void Spawned()
    {
        lifetime = TickTimer.CreateFromSeconds(Runner, 20.0f);

        isInitialized = false;

        isInitialized = true;
        
    }


    public static void OnInitializeCube(Changed<CubeNetworked> changed)
    {
        bool isInitializedCurrently = changed.Behaviour.isInitialized;

        changed.LoadOld();
        bool isInitializedOld = changed.Behaviour.isInitialized;

        if(isInitializedCurrently && !isInitializedOld)
        {
            changed.Behaviour.initializeMesh();
        }
    }

    private void initializeMesh()
    { 
        meshFilter = this.GetComponent<MeshFilter>();

        Vector3[] vertices = new Vector3[] {new Vector3(0,0,0),new Vector3(2,0,0),
            new Vector3(2,2,0), new Vector3(0,2,0),
            new Vector3(0,2,2), new Vector3(2,2,2),
            new Vector3(2,0,2), new Vector3(0,0,2)
        };
        Vector2[] UV = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        int[] trianagles = new int[] { 0,2,1,
            0,3,2,
            2,3,4,
            2,4,5,
            1,2,5,
            1,5,6,
            0,7,4,
            0,4,3,
            5,4,7,
            5,7,6,
            0,6,7,
            0,1,6};
        Vector3[] normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };

        var mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = trianagles;
        mesh.Optimize();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        
        GetComponent<NetworkTransform>().InterpolationTarget = this.transform;

        
    }

    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void Awake()
    {
        
    }
}
