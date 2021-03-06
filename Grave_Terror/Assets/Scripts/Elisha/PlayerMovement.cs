// Author: Elisha Anagnostakis
// Date Modified: 29/11/18
// Purpose: This script handles both players movement in the world such as rotation, forward and backwards movement 

using System.Collections.Generic;
using UnityEngine;
using XboxCtrlrInput;

public class PlayerMovement : MonoBehaviour
{
    // Script references
    public XboxControllerManager xboxController;
    private CharacterController characterControl;

    // Rotation and forward movement
    private Vector3 prevRotDirection = Vector3.forward;
    public float rotationSmoothing = 7f;
    public float rotationSpeed;
    public float gravity;
    public float walkSpeed;
    public float maxSpeed;

    // Relative camera rotation
    public Transform cam;
    private Vector3 inputDirection = new Vector3(0, 0, 0);
    private Vector3 moveDirection = new Vector3(0, 0, 0);
    private Vector3 directionVector = new Vector3(0, 0, 0);

    // Animation
    public Animator anim;

    //Audio
    public List<AudioClip> footSteps = new List<AudioClip>();
    private AudioSource audioSource;
    public float stepInterval = 0.5f;
    private float stepTimer = 0.0f;
    public float minPitch = 0.0f;
    public float maxPitch = 1.0f;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        characterControl = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
    }

    // Move function that lets the player use the controller walk around the world
    public void Move()
    {
        // checks if the xbox controller is plugged in
        if (xboxController.useController == true)
        {
            // gets x axis of the left joystickX
            float axisX = XCI.GetAxisRaw(XboxAxis.LeftStickX, xboxController.controller);
            // gets z axis of the left joystickY
            float axisZ = XCI.GetAxisRaw(XboxAxis.LeftStickY, xboxController.controller);

            // Handles the blend Tree animation window for players direction of movement and playing the specific animation accordingly
            Quaternion inputRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            Vector3 animDirection = inputRotation * (Vector3.Scale(cam.eulerAngles, new Vector3(0,1,0)) + new Vector3(axisX, 0, axisZ));
            Debug.DrawLine(transform.position, transform.position + animDirection * 2.0f, Color.magenta);

            Debug.Log(transform.eulerAngles.y);
            if (transform.eulerAngles.y > 60.0f && transform.eulerAngles.y < 120.0f)
                animDirection = -animDirection;
            if (transform.eulerAngles.y > 230.0f && transform.eulerAngles.y < 330.0f)
                animDirection = -animDirection;

            // animations
            anim.SetFloat("DirectionX", animDirection.x);
            anim.SetFloat("DirectionY", animDirection.z);

            // Camera relative movement 
            inputDirection = new Vector3(axisX, 0, axisZ);
            Vector3 camForward = cam.forward;
            camForward.y = 0;
            camForward = camForward.normalized;

            Quaternion cameraRotation = Quaternion.FromToRotation(Vector3.forward, camForward);
            Vector3 lookForward = cameraRotation * inputDirection;
            // if the player is moving
            if (inputDirection.sqrMagnitude > 0)
            {
                // play anaiomation
                anim.SetBool("IsMoving", true);
                // camera relative
                Ray look = new Ray(transform.position, lookForward);
                transform.LookAt(look.GetPoint(1));

                stepTimer += Time.deltaTime;
                
            }
            else
            {
                anim.SetBool("IsMoving", false);
            }
            // adds the walk speed to the movement 
            moveDirection = transform.forward * walkSpeed * inputDirection.sqrMagnitude;
            // caps the walk speed 
            if(walkSpeed >= maxSpeed)
            {
                walkSpeed = maxSpeed;
            }

            // adds gravity to the player so they dont float and can fall off edges
            moveDirection.y = moveDirection.y - (gravity * Time.deltaTime);
            // Plays the main character controller movement function
            characterControl.Move(moveDirection * Time.deltaTime);

            // plays foot step audio 
            if (inputDirection.sqrMagnitude > 0.1)
            {
                if (stepTimer >= stepInterval)
                {
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(footSteps[Random.Range(0, footSteps.Count)]);
                    stepTimer = 0;

                }
            }
        }
    }

    // Rotation of player
    public void Turning()
    {
        // checks if the controller is being used
        if(xboxController.useController)
        {
            float rotateAxisX = XCI.GetAxisRaw(XboxAxis.RightStickX, xboxController.controller);
            float rotateAxisZ = XCI.GetAxisRaw(XboxAxis.RightStickY, xboxController.controller);

            // puts the rotation x and z axis in a new vector
            directionVector = new Vector3(rotateAxisX, 0, rotateAxisZ);

            if (directionVector.magnitude < 0.1f)
            {
                directionVector = prevRotDirection;
            }

            directionVector = directionVector.normalized;
            prevRotDirection = directionVector;
            transform.localRotation = Quaternion.LookRotation(directionVector);
        }
    }
}