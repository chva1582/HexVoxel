using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMovement {Ground , Frozen, Flying };

public class Player : MonoBehaviour
{
    Rigidbody body;
    public PlayerMovement playerMovement;
    public float rotationalSpeed;
    public float linearSpeed;
    public float jumpStrength;
    bool groundContact;
    int timer;
    Vector3 oldMouse, currentMouse;

    Transform cameraTransform;

	// Use this for initialization
	void Start ()
    {
        body = gameObject.GetComponent<Rigidbody>();
        cameraTransform = transform.GetChild(0).transform;
	}
	
	// Update is called once per frame
	void Update ()
    {
        MovementControl();
        TimerCheck();
        if (Input.GetButton("Fire2"))
            CameraRotation();
        else
            oldMouse = Vector3.zero;
        if (playerMovement == PlayerMovement.Flying)
            body.useGravity = false;
        else
            body.useGravity = true;
    }

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
        currentMouse = Input.mousePosition;
        if (oldMouse == Vector3.zero)
        {
            oldMouse = Input.mousePosition;
            return;
        }
        transform.Rotate(new Vector3(0, (oldMouse.x - currentMouse.x) * rotationalSpeed, 0));
        Vector3 oldCamera = cameraTransform.localRotation.eulerAngles;
        cameraTransform.Rotate(new Vector3((oldMouse.y - currentMouse.y) * -0.5f * rotationalSpeed, 0, 0));
        oldMouse = Input.mousePosition;

    }
}
