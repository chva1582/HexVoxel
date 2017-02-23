using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody body;
    public float rotationalSpeed;
    public float linearSpeed;
    public float jumpStrength;
    bool groundContact;
    int timer;

	// Use this for initialization
	void Start () {
        body = gameObject.GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.D))
            body.AddTorque(transform.up * rotationalSpeed);
        if (Input.GetKey(KeyCode.A))
            body.AddTorque(transform.up * -1 * rotationalSpeed);
        if (Input.GetKey(KeyCode.W) && groundContact)
            body.AddForce(transform.forward * linearSpeed);
        if (Input.GetKey(KeyCode.S) && groundContact)
            body.AddForce(transform.forward * -1 * linearSpeed);
        if (Input.GetKeyDown(KeyCode.Space) && groundContact)
            body.AddForce(transform.up * jumpStrength);
        if (timer == 100)
        {
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            timer++;
        }
        else if (timer < 100)
            timer++;
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
}
