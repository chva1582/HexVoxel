//Controls for the Player Avatar
//Assigned to the Player GameObject
using UnityEngine;

public enum PlayerMovement {Ground, Frozen, Flying };

public class Player : MonoBehaviour
{
    #region Instantiation
    Rigidbody body;
    public PlayerMovement playerMovement;
    public World world;
    public float rotationalSpeed;
    public float linearSpeed;
    public float jumpStrength;
    bool groundContact;
    int timer;

    Transform cameraTransform;
    #endregion

    #region Start & Update
    // Use this for initialization
    void Start ()
    {
        body = gameObject.GetComponent<Rigidbody>();
        cameraTransform = transform.GetChild(0).transform;
        Cursor.lockState = CursorLockMode.Locked;
	}

    // Update is called once per frame
    void Update()
    {
        MovementControl();
        TimerCheck();
        if (Input.GetButton("Fire1"))
        {
            RaycastHit hit;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 5))
                print(hit.point.ToString());
        }

        body.useGravity = !(playerMovement == PlayerMovement.Flying);

        if (Input.GetKeyDown(KeyCode.P))
            Cursor.lockState = ((Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked);
        if(Cursor.lockState == CursorLockMode.Locked)
            CameraRotation();
    }

    void TimerCheck()
    {
        if (timer == 100)
        {
            if (playerMovement == PlayerMovement.Ground)
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (playerMovement == PlayerMovement.Frozen)
                body.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (playerMovement == PlayerMovement.Flying)
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            timer++;
        }
        else if (timer < 100)
            timer++;
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
            print(hit.point.ToString());
            Chunk chunk = world.GetChunk(hit.point);
            HexCoord hexUnrounded = chunk.PosToHex(hit.point);
            HexCell hexCenter = hexUnrounded.ToHexCell();
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    for (int k = -2; k <= 2; k++)
                    {

                        HexCell hex = new HexCell(hexCenter.x + i, hexCenter.y + j, hexCenter.z + k);
                        Vector3 point = chunk.HexToPos(hex);
                        Vector3 c = point - hexUnrounded.ToVector3();
                        float distanceStrength = 1 / (Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2));
                        Vector3 changeNormal = new Vector3(-2 * c.x / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)),
                            -2 * c.y / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)),
                            -2 * c.z / (Mathf.Pow(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2), 2)));
                        chunk.EditPointValue(hex, distanceStrength);
                        chunk.EditPointNormal(hex, changeNormal);
                    }
                }
            }
        }
    }

    void MovementControl()
    {
        if ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) && ((playerMovement == PlayerMovement.Ground) ? groundContact : true))
            body.AddForce(transform.right * linearSpeed);
        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) && ((playerMovement == PlayerMovement.Ground) ? groundContact : true))
            body.AddForce(transform.right * -1 * linearSpeed);
        if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && ((playerMovement == PlayerMovement.Ground) ? groundContact : true))
            body.AddForce(transform.forward * linearSpeed);
        if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && ((playerMovement == PlayerMovement.Ground) ? groundContact : true))
            body.AddForce(transform.forward * -1 * linearSpeed);
        if (Input.GetKey(KeyCode.Space) && ((playerMovement == PlayerMovement.Ground) ? groundContact : true))
            body.AddForce(transform.up * jumpStrength);
        if (Input.GetKey(KeyCode.RightShift) && playerMovement == PlayerMovement.Flying)
            body.AddForce(transform.up * -1 * jumpStrength);
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
