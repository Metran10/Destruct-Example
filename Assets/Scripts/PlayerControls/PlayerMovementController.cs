using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerMovementController : NetworkBehaviour
{
    Camera playerCam;
    NetworkCharacterControllerPrototypeCustomized charController;
    public Transform firepoint;
    public Missile missilePF;
    public Missile strongMissilePF;
    public NetworkObject cubePF;

    [Networked]
    public TickTimer missileCD { get; set; }
    [Networked]
    public TickTimer cubeSpawnCD { get; set; }


    Vector2 viewInput;
    float verticalCameraRotation = 0;

    private void Awake()
    {
        charController = GetComponent<NetworkCharacterControllerPrototypeCustomized>();
        playerCam = GetComponentInChildren<Camera>();
    }


    void Update()
    {
        verticalCameraRotation += viewInput.y * Time.deltaTime * charController.verticalViewSpeed;
        verticalCameraRotation = Mathf.Clamp(verticalCameraRotation, -90, 90);

        playerCam.transform.localRotation = Quaternion.Euler(verticalCameraRotation, 0, 0);
    }


    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkInputData data))
        {

            Vector3 direction = transform.forward * data.movementDir.y + transform.right * data.movementDir.x;
            direction.Normalize();

            charController.Move(direction);

            charController.Rotate(data.horizontalRotation);

            if (data.isJumping)
            {
                charController.Jump();
            }

            Quaternion missRot = playerCam.transform.rotation;

            if (missileCD.ExpiredOrNotRunning(Runner))
            {
                if (data.isFiring)
                {
                    missileCD = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(missilePF, firepoint.position, Quaternion.Euler(missRot.eulerAngles.x,missRot.y,missRot.z - 90), Object.InputAuthority,
                        (runner, o) => { o.GetComponent<Missile>().Init(10 * playerCam.transform.forward); });
                }
                else if (data.isFiringStrong)
                {
                    missileCD = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(strongMissilePF, firepoint.position, Quaternion.Euler(missRot.eulerAngles.x, missRot.y, missRot.z - 90), Object.InputAuthority,
                        (runner, o) => { o.GetComponent<Missile>().Init(10 * playerCam.transform.forward); });
                }
            }

            if (cubeSpawnCD.ExpiredOrNotRunning(Runner))
            {
                if (data.isSpawningCube)
                {
                    cubeSpawnCD = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    NetworkObject cube = Runner.Spawn(cubePF, Object.transform.position + transform.forward * 6, Object.transform.rotation, Object.InputAuthority,
                        (runner, o) => { PrepareObjectCubeTest(o); });
                }

            }

        }

    }

    public void SetViewVector(Vector2 view)
    {
        this.viewInput = view;
    }

    public static void PrepareObjectCubeTest(NetworkObject ob)
    {
        Debug.Log("preparing object");
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
        ob.GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.triangles = trianagles;
        
        mesh.RecalculateNormals();


        ob.gameObject.AddComponent<Rigidbody>();
        ob.gameObject.AddComponent<NetworkRigidbody>();

        ob.gameObject.AddComponent<BoxCollider>();
    }


}
