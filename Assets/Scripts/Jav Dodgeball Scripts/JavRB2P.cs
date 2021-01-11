using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

//features to add:
//Countdown timer after reset
//Ragdoll to protect
//UI for score
//Penalty for missing ragdoll/wall (ex dropping to floor)

namespace OpenCVForUnityExample
{
    public class JavRB2P : MonoBehaviour
    {
        //mats are used as indicators for game phase and need to be available throughout script
        public Material blueMat;
        public Material redMat;
        public Material whiteMat;

        private Rigidbody rb; //initialize javelin rigid body
        public Pink_track JavControl;//we will be pulling player positions from Pink_track script
  
        public Transform R_Wrist;//green team object
        public Transform L_Wrist;//red team object

        public GameObject javFab;//javelin prefab

        public float throwThresh=9;//throw threshold, javelin is released here
        float moveThresh;//keeps player objects from jumping around as the result of tracking errors

        //left(red) and right (green) team positions
        int LeftY;
        int LeftX;
        int RightY;
        int RightX;

        int count = 0;//tracking frames that the javelin is the air to enable a timeout feature

        Quaternion straight = Quaternion.Euler(new Vector3(90, 0, 0)); //used to align javelin at start

        //booleans to track game state
        bool Left=false;
        bool Right = false;
        bool Ready = false;
        bool Thrown = false;

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name == "Wall") //if we hit the wall, reset!
            {
                reset();
            }
        }

        void Start()
        {
            //initialize javelin
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.MoveRotation(straight);
        }
        void FixedUpdate()
        {
            //Player Object position control
            R_Wrist.localPosition = new Vector3(0f, RightY * (.03f), RightX * (.03f));
            L_Wrist.localPosition = new Vector3(0f, LeftY * (.03f), LeftX * (.03f));

            if (R_Wrist.localPosition.z < moveThresh)
            {
                R_Wrist.localPosition = new Vector3(0f, RightY * (.03f), moveThresh);
            }

            if (L_Wrist.localPosition.z > -moveThresh)
            {
                L_Wrist.localPosition = new Vector3(0f, LeftY * (.03f), -moveThresh);
            }

            //pull tracked positions from Pink_track script
            RightY = -JavControl.ball2Y;
            RightX = JavControl.ball2X;
            LeftY = -JavControl.ball1Y;
            LeftX = JavControl.ball1X;

            if (Thrown)
            {
                count = count + 1;
                if (count > 200)//if the javelin gets stuck in flight, or we lose it over the wall...
                {
                    reset();//..it will time out and reset
                }

                moveThresh = 1;

                if (Right) //if right just threw
                {
                    //look for left to grab it
                   if (IsBetween(L_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                   IsBetween(L_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        rb.isKinematic = true;//when kinematic, the javelin will move like a projectile, based on the bodies that act on it
                        Right = false;
                        Thrown = false;
                        Left = true;
                        count = 0;
                    }

                }
                if (Left) //if left just threw
                {
                    //look for right grab
                    if (IsBetween(R_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                        IsBetween(R_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        rb.isKinematic = true;
                        Left = false;
                        Thrown = false;
                        Right = true;
                        count = 0;
                    }
                }
            }
            else //thrown=false
            {
                moveThresh = -1;
                if (Left == false && Right == false) //no one has grabbed it yet...
                {
                    //..so who is going to grab it?
                    //left?
                    if (IsBetween(L_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                    IsBetween(L_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        Left = true;
                        Debug.Log("L First");
                    }
                    //or right?
                    if (IsBetween(R_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                        IsBetween(R_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        Debug.Log("R First");
                        Right = true;
                    }  
                }

                //move jav with corresponding hand
                if (Left)
                {
                    rb.MovePosition(L_Wrist.localPosition);
                    if (rb.position.z < -10)//player has "tagged" their wall, making them ready to throw
                    {                         
                        Ready = true;
                        rb.GetComponent<Renderer>().material = redMat;
                    }
                    if (Ready)
                    {
                        if (rb.position.z >-throwThresh)//if the jav crosses the corresponding thresh
                        {
                            Debug.Log("Throw");
                            Thrown = true; //throw it!
                            Debug.Log("Left: " + Left);
                            Debug.Log("Right: " + Right);
                            Debug.Log(count);
                            rb.isKinematic = false;
                            Ready = false;
                        }
                    }
                }
                if (Right)
                {
                    rb.MovePosition(R_Wrist.localPosition);
                    if (rb.position.z > 10)//player has "tagged" their wall, making them ready to throw
                    {
                        Ready = true;
                        rb.GetComponent<Renderer>().material = blueMat;
                    }
                    if (Ready)
                    {
                        if (rb.position.z < throwThresh)//if the jav crosses the corresponding thresh
                        {
                            Debug.Log("Throw");
                            Debug.Log("Left: " + Left);
                            Debug.Log("Right: " + Right);
                            Thrown = true;//throw it!
                            Debug.Log(count);
                            rb.isKinematic = false;
                            Ready = false;
                        }
                    }
                }
            }
        }

        private void reset()
        {
            //reset booleans
            Thrown = false;
            Right = false;
            Left = false;
            Debug.Log("reset");
            rb.isKinematic = true;

            //add a new javelin prefab
            GameObject stuck = Instantiate(javFab, rb.position, rb.rotation);
            stuck.GetComponent<Renderer>().material = rb.GetComponent<Renderer>().material;
            rb.GetComponent<Renderer>().material = whiteMat;
            rb.MoveRotation(straight);
            rb.MovePosition(new Vector3(0f,0f,0f));
            count=0; 
        }

        public bool IsBetween(double testValue, double bound1, double bound2)//used to test for a user "grabbing" the javelin
        {
            if (bound1 > bound2)
                return testValue >= bound2 && testValue <= bound1;
            return testValue >= bound1 && testValue <= bound2;
        }
    }
}