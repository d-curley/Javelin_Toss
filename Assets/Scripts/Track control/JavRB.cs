using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace OpenCVForUnityExample
{
    public class JavRB : MonoBehaviour
    {
        float thrustX;
        float thrustY;

        private Rigidbody rb; // do i need this?
        public Track_Control_Jav JavControl;
        private Vector3 Displacement;
        public Transform RightTarget;

        public GameObject javFab;

        public float forceAmp;

        bool thrown = false;
        bool grabbed = false;

        int javY;
        int javX;

        int lastY=0;
        int lastX=0;
        Quaternion straight = Quaternion.Euler(new Vector3(90, 0, 0));

        public int thrownThresh = -4;
        public int hitThresh = 14;

        private Vector3 JavStart;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            JavStart = new Vector3(0f, 5f, -13f);
        }

        void OnCollisionEnter(Collision collision)
        {

            reset();
            
        }

        //rb.position.y;
        //rb.MovePosition(move);
        //rb.AddForce(0, 10, 0, ForceMode.Impulse);

        void Update()
        {
            javY = -JavControl.ball1Y;
            javX = JavControl.ball1X;

            thrustX = forceAmp*(javX - lastX);
            thrustY = forceAmp*(javY - lastY);

            RightTarget.localPosition = new Vector3(0f, javY * (.03f), javX * (.036f));

            if (thrown == false)
            {
                if (RightTarget.localPosition.y > JavStart.y && RightTarget.localPosition.z < JavStart.z+1.25f)
                {
                    grabbed = true;
                }
                if (grabbed == true)
                {
                    rb.MovePosition(RightTarget.localPosition);
                }
                else
                {
                    rb.MovePosition(JavStart);
                }

                if (grabbed == true && rb.position.z > thrownThresh)
                {
                    rb.isKinematic = false;
                    rb.AddForce(0, thrustY, thrustX, ForceMode.Impulse);
                    Debug.Log("Thrown");
                    thrown = true;
                }
            }
            else
            {
                if (rb.position.z > hitThresh)
                {
                    reset();
                }

            }

            lastX = javX;
            lastY = javY;

        }
        private void reset()
        {
            rb.isKinematic = true;
            GameObject stuck = Instantiate(javFab, rb.position, rb.rotation);
            rb.MoveRotation(straight);
            thrown = false;
            grabbed = false;
            
            
        }
    }  
}