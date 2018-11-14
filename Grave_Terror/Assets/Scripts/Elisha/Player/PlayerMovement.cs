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
    public AnimationCurve dodgeCurve;
    public float dodgeSpeed;
    public float dodgeTime;
    private float dodgeTimer = 0.0f;
    public float trailTime;
    public float trailDamage;
    public GameObject flameTrail;
    public XboxControllerManager xboxController;
    private Camera camRotationY;
    private Vector3 prevRotDirection = Vector3.forward;
    public float walkSpeed;
    public float maxSpeed;

    // Relative camera rotation
    public Transform cam;
    private Vector3 inputDirection = new Vector3(0, 0, 0);
    private Vector3 moveDirection = new Vector3(0, 0, 0);
    private CharacterController characterControl;

    // Animation
    public new Animator anim;

    // clamp
    public float maxDistance;
    public Transform player2;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        floorMask = LayerMask.GetMask("Floor");
        characterControl = GetComponent<CharacterController>();
        camRotationY = GetComponent<Camera>();
    }

    private void Update()
    {
        Clamp();
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

            transform.position += moveDirection.normalized * dodgeCurve.Evaluate(dodgeTimer / dodgeTime) * dodgeSpeed * Time.deltaTime;
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
        // damage the enemy.
        if (other.gameObject.tag == "Enemy")
        {
            other.gameObject.GetComponent<Enemy>().Ignite(trailDamage);
            print("Damaging enemy " + trailDamage);
        }
    }

    void Clamp()
    {
        Vector3 store1 = transform.position + moveDirection;
        Vector3 store2 = player2.position;
        store1.y = 0;
        store2.y = 0;

        float storef = Vector3.Distance(store1, store2);

        if (storef >= maxDistance)
        {
            Vector3 storeAvg = (store1 + store1) / 2.0f;
            transform.position = Vector3.ClampMagnitude(transform.position - storeAvg, maxDistance / 2.0f) + storeAvg;
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
       
        //if (XCI.GetButtonDown(XboxButton.LeftStick, xboxController.controller))
        //{
        //    isDodging = true;
        //    anim.SetBool("IsDashing", true);
        //    flameTrail.GetComponentInChildren<ParticleSystem>().Play();
        //}
    }

    // Rotation of player
    public void Turning()
    {
        if(xboxController.useController)
        {
            float rotateAxisX = XCI.GetAxisRaw(XboxAxis.RightStickX, xboxController.controller);
            float rotateAxisZ = XCI.GetAxisRaw(XboxAxis.RightStickY, xboxController.controller);

            Vector3 directionVector = new Vector3(rotateAxisX, 0, rotateAxisZ);

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
    }
}