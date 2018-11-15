using UnityEngine;
using XboxCtrlrInput;

public class PlayerMovement : MonoBehaviour
{
    private Vector3 offset;
    private int floorMask;
    private float camRayLength = 100f;
    public float rotationSmoothing = 7f;
    public float rotationSpeed;
    public float gravity;
    private bool isDodging = false;
    //public AnimationCurve dodgeCurve;
    public float dodgeSpeed;
    public float dodgeTime;
    private float dodgeTimer = 0.0f;
    public float trailTime;
    public float trailDamage;
    public GameObject flameTrail;
    public XboxControllerManager xboxController;
    private Vector3 prevRotDirection = Vector3.forward;
    public float walkSpeed;
    public float maxSpeed;

    // Relative camera rotation
    public Transform cam;
    private Vector3 inputDirection = new Vector3(0, 0, 0);
    private Vector3 moveDirection = new Vector3(0, 0, 0);
    private Vector3 directionVector = new Vector3(0, 0, 0);
    private CharacterController characterControl;

    // Animation
    public Animator anim;

    // clamp
    public float maxDistance;
    public Transform player2;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        floorMask = LayerMask.GetMask("Floor");
        characterControl = GetComponent<CharacterController>();
    }

    public void Dashing()
    {
        // If player is dodging
        if (isDodging)
        {

            // start dodge and fire trail timers
            dodgeTimer += Time.deltaTime;
            if (dodgeTimer >= dodgeTime)
            {
                isDodging = false;
                anim.SetBool("IsDashing", false);
                flameTrail.GetComponent<ParticleSystem>().Stop();
                dodgeTimer = 0.0f;
            }

            transform.position += moveDirection.normalized * dodgeSpeed * Time.deltaTime;
            moveDirection.y = 0;
        }
        else
        {
            Move();
        }

        if (XCI.GetButtonDown(XboxButton.LeftStick, xboxController.controller))
        {
            isDodging = true;
            anim.SetBool("IsDashing", true);
            flameTrail.GetComponentInChildren<ParticleSystem>().Play();
        }
    }

    // if a enemy runs into the fire trail
    public void OnTriggerEnter(Collider other)
    {
        if(isDodging)
        {
            // damage the enemy.
            if (other.gameObject.tag == "Enemy")
            {
                other.gameObject.GetComponent<Enemy>().Ignite(trailDamage);
                print("Damaging enemy " + trailDamage);
            }
        }
        else
        {
            isDodging = false;
        }
    }

    public void Move()
    {
        if (xboxController.useController == true)
        {

            float axisX = XCI.GetAxisRaw(XboxAxis.LeftStickX, xboxController.controller);
            float axisZ = XCI.GetAxisRaw(XboxAxis.LeftStickY, xboxController.controller);

            inputDirection = new Vector3(axisX, 0, axisZ);
            Vector3 camForward = cam.forward;
            camForward.y = 0;
            camForward = camForward.normalized;

            Quaternion cameraRotation = Quaternion.FromToRotation(Vector3.forward, camForward);
            Vector3 lookForward = cameraRotation * inputDirection;

            if (inputDirection.sqrMagnitude > 0)
            {
                anim.SetBool("IsMoving", true);

                Ray look = new Ray(transform.position, lookForward);
                transform.LookAt(look.GetPoint(1));
            }
            else
            {
                anim.SetBool("IsMoving", false);
            }

            moveDirection = transform.forward * walkSpeed * inputDirection.sqrMagnitude;

            if(walkSpeed >= maxSpeed)
            {
                walkSpeed = maxSpeed;
            }

            moveDirection.y = moveDirection.y - (gravity * Time.deltaTime);
            characterControl.Move(moveDirection * Time.deltaTime);

        }
        else if (!xboxController.useController)
        {
            if (Input.GetKey(KeyCode.W))
            {
                transform.position += new Vector3(walkSpeed * Time.deltaTime, 0, 0);
                anim.SetBool("IsMoving", true);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                transform.position += new Vector3(0, 0, walkSpeed * Time.deltaTime);
                anim.SetBool("IsMoving", true);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                transform.position += new Vector3(-walkSpeed * Time.deltaTime, 0, 0);
                anim.SetBool("IsMoving", true);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                transform.position += new Vector3(0, 0, -walkSpeed * Time.deltaTime);
                anim.SetBool("IsMoving", true);
            }
            else
            {
                anim.SetBool("IsMoving", false);
            }
        }
    }

    // Rotation of player
    public void Turning()
    {
        if(xboxController.useController)
        {
            float rotateAxisX = XCI.GetAxisRaw(XboxAxis.RightStickX, xboxController.controller);
            float rotateAxisZ = XCI.GetAxisRaw(XboxAxis.RightStickY, xboxController.controller);

            directionVector = new Vector3(rotateAxisX, 0, rotateAxisZ);

            if (directionVector.magnitude < 0.1f)
            {
                directionVector = prevRotDirection;
            }

            directionVector = directionVector.normalized;
            prevRotDirection = directionVector;
            transform.rotation = Quaternion.LookRotation(directionVector);

        }
        else if (!xboxController.useController)
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit floorHit;

            if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
            {
                Vector3 playerToMouse = floorHit.point - transform.position;
                playerToMouse.y = 0f;

                Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

                Vector3 position = transform.position + offset;
                // smoothing of the rotation of player
                transform.position = Vector3.Lerp(transform.position, position, rotationSmoothing * Time.deltaTime);
            }
        }

        Aim();
    }

    Vector3 hitLocation = Vector3.zero;

    void Aim()
    {
        //Raycasting from the player's position and creating an array of hit results from players forward direction
        RaycastHit[] hit;
        hit = Physics.RaycastAll(transform.position, transform.forward, 100.0f);
        Debug.DrawRay(transform.position, transform.forward, Color.magenta);
        foreach(RaycastHit result in hit)
        {
            //If the cast hits an object tagged as 'enemy', run settargeted function on Enemy script
            if (result.collider.gameObject.tag == "Enemy")
            {
                hitLocation = result.point;
                result.collider.gameObject.GetComponent<Enemy>().SetTargeted();

                break;
            }
        }
    }

    //Debug cube on targeted location
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(hitLocation, Vector3.one);
    }
}