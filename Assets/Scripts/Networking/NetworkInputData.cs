using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementDir;
    public NetworkBool isJumping;
    public float horizontalRotation;
    public NetworkBool isFiring;


    public NetworkBool isSpawningCube;



}
