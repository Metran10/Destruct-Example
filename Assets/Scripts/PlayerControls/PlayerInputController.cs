using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    Vector2 moveInput = Vector2.zero;
    Vector2 viewInput = Vector2.zero;
    bool isJumping = false;
    bool isFiring = false;
    bool isSpawningCube = false;



    PlayerMovementController playerMovementController;

    private void Awake()
    {
        playerMovementController = GetComponent<PlayerMovementController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            isJumping = true;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            isFiring = true;
            //Debug.Log("Is firiiing");
        }

        viewInput.x = Input.GetAxis("Mouse X");
        viewInput.y = -1 * Input.GetAxis("Mouse Y");

        if (Input.GetKey(KeyCode.E))
        {
            isSpawningCube = true;
        }


        playerMovementController.SetViewVector(viewInput);

    }



    public NetworkInputData GetPlayerInputData()
    {
        NetworkInputData data = new NetworkInputData();

        data.movementDir = moveInput;
        data.horizontalRotation = viewInput.x;
        data.isJumping = isJumping;
        data.isFiring = isFiring;
        data.isSpawningCube = isSpawningCube;

        isSpawningCube = false;
        isJumping = false;
        isFiring = false;

        return data;
    }





}
