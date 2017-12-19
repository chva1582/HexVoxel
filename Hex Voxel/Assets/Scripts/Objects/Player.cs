//Controls for the Player Avatar
//Assigned to the Player GameObject
using System.Collections;
using UnityEngine;

public enum PlayerMovement {Grounded, Frozen, Flying };

public class Player : MonoBehaviour
{
    #region Variables
    //Components
    public World world;
    Rigidbody body;
    Transform cameraTransform;

    //Public Variables
    public PlayerMovement playerMovement;
    public float rotationalSpeed;
    public float linearSpeed;
    public float ascensionSpeed;
    public float jumpStrength;

    //Status Booleans
    bool groundContact;
    bool jumpReady = true;

    //Mode Properties
    bool Grounded { get { return playerMovement == PlayerMovement.Grounded; } }
    bool Frozen { get { return playerMovement == PlayerMovement.Frozen; } }
    bool Flying { get { return playerMovement == PlayerMovement.Flying; } }
    #endregion

    #region Start & Update
    // Use this for initialization
    void Start ()
    {
        body = gameObject.GetComponent<Rigidbody>();
        cameraTransform = transform.GetChild(0).transform;
        Cursor.lockState = CursorLockMode.Locked;
        StartCoroutine(DelayedRuleEnforcement());
	}

    // Update is called once per frame
    void Update()
    {
        MovementControl();
        if (Input.GetButton("Fire1"))
        {
            PointEdit();
        }

        body.useGravity = !Flying;

        if (Input.GetKeyDown(KeyCode.P))
            Cursor.lockState = ((Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked);
        if(Cursor.lockState == CursorLockMode.Locked)
            CameraRotation();
    }
    #endregion

    #region Coroutines
    IEnumerator DelayedRuleEnforcement()
    {
        yield return new WaitForSeconds(0.1f);
        if (playerMovement == PlayerMovement.Grounded)
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (playerMovement == PlayerMovement.Frozen)
            body.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (playerMovement == PlayerMovement.Flying)
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    IEnumerator HoldForNextJump()
    {
        jumpReady = false;
        yield return new WaitForSeconds(1f);
        jumpReady = true;
    }
    #endregion

    #region Collision
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
            groundContact = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
            groundContact = false;
    }
    #endregion

    #region Controls
    void PointEdit()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 5))
        {
            Chunk chunk = world.GetChunk(hit.point);
            HexCoord hexUnrounded = chunk.PosToHex(hit.point);
            HexCell hexCenter = hexUnrounded.ToHexCell();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {

                        HexCell hex = new HexCell(hexCenter.X + i, hexCenter.Y + j, hexCenter.Z + k);
                        Vector3 point = chunk.HexToPos(hex);
                        Vector3 c = 2 * point - hexUnrounded.ToVector3();
                        float distanceStrength = 10 / (Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2));
                        Vector3 changeNormal = 10 * new Vector3(-2 * c.x / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)),
                            -2 * c.y / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)),
                            -2 * c.z / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)));
                        chunk.EditPointValue(hex, distanceStrength);
                        chunk.EditPointNormal(hex, changeNormal);
                        gameObject.GetComponent<LoadChunks>().AddToUpdateList(chunk.chunkCoords);
                    }
                }
            }
        }
    }

    void MovementControl()
    {
        int right = ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) && (Grounded ? groundContact : true)) ? 1 : 0;
        int left = ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) && (Grounded ? groundContact : true)) ? 1 : 0;
        int forward = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && (Grounded ? groundContact : true)) ? 1 : 0;
        int backward = ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && (Grounded ? groundContact : true)) ? 1 : 0;
        Move(((right - left) * transform.right + (forward - backward) * transform.forward) * linearSpeed);

        if (Flying)
        {
            int up = Input.GetKey(KeyCode.Space) ? 1 : 0;
            int down = (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)) ? 1 : 0;
            Move((up - down) * transform.up * ascensionSpeed);
        }
        else if(Grounded)
        {
            if (Input.GetKey(KeyCode.Space) && groundContact && jumpReady)
                Jump();
        }
    }

    void Move(Vector3 dir)
    {
        body.AddForce(dir, ForceMode.Impulse);
    }

    void Jump()
    {
        body.AddForce(transform.up * jumpStrength / (Flying ? 20 : 1), ForceMode.Impulse);
        StartCoroutine(HoldForNextJump());
    }

    void CameraRotation()
    {
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * rotationalSpeed, 0));
        Vector3 oldCamera = cameraTransform.localRotation.eulerAngles;
        float xRot = cameraTransform.rotation.eulerAngles.x > 180 ? cameraTransform.rotation.eulerAngles.x - 360 : cameraTransform.rotation.eulerAngles.x;
        if(!(xRot > 80 && (Input.GetAxis("Mouse Y") < 0)) && !(xRot < -80 && (Input.GetAxis("Mouse Y") > 0)))
            cameraTransform.Rotate(new Vector3(Input.GetAxis("Mouse Y") * -0.5f * rotationalSpeed, 0, 0));
    }
    #endregion
}
