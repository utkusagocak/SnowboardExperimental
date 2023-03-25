using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using TMPro;
public enum BoardingStates
{
  Skate,
  Break,
  Jump,
}

public class BoardMove : MonoBehaviour
{
  private Vector3 m_surfaceNormal = new Vector3();
  private Vector3 m_collisionPoint = new Vector3();
  private bool m_onSurface;
  private float minSpeed = 0f;
  private float maxSpeed = 30f;
  private float speed = 0f;
  private float RotationUnit = 1f;
  public float jumpStrength = 5;
  private Rigidbody rb;
  private Vector3 slopeDirection;

  private float yRotation = 0;
  private Vector3 velocity;
  public BoardingStates state = BoardingStates.Skate;

  public float groundOffset = 0.1f;
  private float characterHeight;
    void Start()
  {
    rb = GetComponent<Rigidbody>();
  }

  void Update()
  {
        //Debug.DrawRay(transform.position, GetSlopeDirection() * 3, Color.green);
        //Debug.DrawRay(transform.position, rb.velocity * 3, Color.green);
        if (m_onSurface)
    {
      state = BoardingStates.Skate;
      // A/D Key: Change Board direction
      yRotation = Input.GetAxisRaw("Horizontal");
            rb.AddForce(slopeDirection.normalized * 5, ForceMode.Acceleration);
      // Left Shift (Down): Change State as break, slow down speed and rotate board for break animation.

      Jump();
      //if (Input.GetKeyDown(KeyCode.LeftShift))
      //{
      //    state = BoardingStates.Break;
      //    //transform.DOLocalRotate(new Vector3(0, 90, 0), 1f, RotateMode.LocalAxisAdd);
      //    transform.DOLocalRotateQuaternion(Quaternion.Euler(0, 90, 0), 1f);
      //}
      //// Left Shift (Up): Change State as Skate, rotate board as rotation before break.
      //else if (Input.GetKeyUp(KeyCode.LeftShift))
      //{
      //    transform.DOLocalRotate(new Vector3(0, -90, 0), 1f, RotateMode.LocalAxisAdd).OnComplete(() =>
      //    {
      //        state = BoardingStates.Skate;
      //    });
      //}

      // Space Key: Increase speed. Mostly for debugging.
      if (Input.GetKey(KeyCode.Space))
      {
        speed = Mathf.Clamp(speed + 0.1f, minSpeed, maxSpeed);
      }

      if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
      {
        //Todo: fixRotatino when board is surface.
        
      }
    }

    if (!m_onSurface && GetHeight() > 2)
    {
      state = BoardingStates.Jump;
                

        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {
            FixRotation();
        }

    }
  }
    void FixedUpdate()
    {
        if (state == BoardingStates.Jump)
        {
          float v = Input.GetAxisRaw("Vertical");
          float h = Input.GetAxisRaw("Horizontal");
          rb.AddTorque(new Vector3(v, yRotation, h) * 10000);
        }

        if (state == BoardingStates.Break)
        {
          // On break state. Decrase speed and stop the board.
          speed = Mathf.Clamp(speed - 0.5f, minSpeed, maxSpeed);
        }
        else
        {
                // Angular Velocity for left-right rotation.
          rb.angularVelocity = (new Vector3(0, yRotation, 0)) * RotationUnit;

          // Skating Physics
          if (OnSlope())
          {
            // Angle Between direction of movement and slope.
            float angle = Vector3.Angle(rb.rotation * (new Vector3(0, 0, 1)), slopeDirection);

            // If direction of movement and slope are similar increase speed, otherwise decrease.
            if (angle < 30 || angle > 330) speed = Mathf.Clamp(speed + 0.1f, minSpeed, maxSpeed);
            else if (angle < 45 || angle > 305) speed = Mathf.Clamp(speed + 0.05f, minSpeed, maxSpeed);
            else if (angle > 45 && angle < 90) speed = Mathf.Clamp(speed - 0.05f, minSpeed, maxSpeed);
            else if (angle > 90 && angle < 270) speed = Mathf.Clamp(speed - 0.2f, minSpeed, maxSpeed);
          }
          else
          {
            if (m_onSurface)
            {
              // If there is no slope, decrease speed.
              speed = Mathf.Clamp(speed - 0.01f, minSpeed, maxSpeed);
            }
            // Todo: Should check if board touching the ground. If it is, we should not lower speed.
          }
    }

        if (BoardingStates.Jump != state)
        {
            velocity = GetVelocity();
            rb.velocity = velocity;
        }

    }

    private void FixRotation()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, -Vector3.up, out hit);
        var localRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            var euler = localRot.eulerAngles;
            euler.y = 0;
            localRot.eulerAngles = euler;
            transform.localRotation = Quaternion.LerpUnclamped(transform.localRotation, localRot, 2 * Time.deltaTime);
    }

    private Vector3 GetVelocity()
    {
    // Calculates velocity. Not modifies y axis, beacuse of default gravity.
    Vector3 currentV = rb.velocity;
    Vector3 newV = rb.rotation * (new Vector3(0, 0, 1) * speed);
    newV.y = 0;
    newV.y = currentV.y;

    return newV;
    }

  public float GetHeight()
  {
        RaycastHit hit;
        Physics.Raycast(transform.position, Physics.gravity, out hit);
        Debug.DrawRay(transform.position, Physics.gravity, Color.red);
        return hit.distance;

  }
  private bool OnSlope()
  {
        if (!m_onSurface) return false;
    // Project a ray from pivot of object in the -y direction.
    // If it is hit to the surface, get slope angle and direction.
    float angle = Vector3.Angle(Vector3.up, m_collisionPoint);
    slopeDirection = Vector3.ProjectOnPlane(new Vector3(0, 0, 1), m_surfaceNormal).normalized;

        Debug.DrawRay(rb.position, slopeDirection * 5, Color.red);
        // If angle of slope bigger then 20. There is a slope that should effect speed of board. 
        return angle >= 20;
  }

  private void OnCollisionStay(Collision collision)
  {
    if (collision.gameObject.CompareTag("IceFloor"))
    {
      m_onSurface = true;
      m_surfaceNormal = collision.GetContact(0).normal;
      m_collisionPoint = collision.GetContact(0).point;

    }

  }
  private void OnCollisionExit(Collision collision)
  {
    if (collision.gameObject.CompareTag("IceFloor"))
    {
      m_onSurface = false;
    }
  }
  void Jump()
  {
    // Jump when the Jump button is pressed and we are on the ground.
    if (Input.GetKeyDown(KeyCode.LeftShift))
    {
      Debug.Log("Jumped");
      rb.AddForce(100 * jumpStrength * Vector3.up);
    }
  }
}
