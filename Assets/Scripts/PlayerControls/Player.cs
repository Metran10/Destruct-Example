using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{

    public static Player localPlayer { get; set; }

    public Transform playerBody;

    Camera playerCam;

    private void Awake()
    {
        playerCam = GetComponentInChildren<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            localPlayer = this;

            AudioListener audio = GetComponentInChildren<AudioListener>();
            audio.enabled = true;

            if(Camera.main != null)
            {
                Camera.main.gameObject.SetActive(false);
            }

            playerCam.enabled = true;


            playerBody.gameObject.layer = LayerMask.NameToLayer("Playermodel");
            foreach (Transform child in playerBody)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Playermodel");
            }


        }
        else
        {
            playerCam.enabled = false;
            AudioListener audio = GetComponentInChildren<AudioListener>();
            audio.enabled = false;
        }

        
        Runner.SetPlayerObject(Object.InputAuthority, Object);

    }




}
