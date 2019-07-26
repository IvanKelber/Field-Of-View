using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    Rigidbody body;
    Camera cam;
    Vector3 velocity;
    public float moveSpeed = 6;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.transform.position.y));
        transform.LookAt(mousePos + Vector3.up*transform.position.y);
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * moveSpeed;

    }

    void FixedUpdate() {
        body.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
    }
}
