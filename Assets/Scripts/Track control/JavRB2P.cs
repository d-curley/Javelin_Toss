using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

//features to add
//Countdown timer after reset
//Ragdoll to protect
//UI for score
//Penalty for missing ragdoll/wall (ex dropping to floor)




namespace OpenCVForUnityExample
{
    public class JavRB2P : MonoBehaviour
    {
        public Material blueMat;
        public Material redMat;
        public Material whiteMat;

        private Rigidbody rb; // do i need this?
        public Pink_track JavControl;
        private Vector3 Displacement;
        private Vector3 JavStart;
        public Transform R_Wrist;
        public Transform L_Wrist;

        public GameObject javFab;

       public float throwThresh=9;
        float moveThresh;

        int LeftY;
        int LeftX;

        int count = 0;

        int RightY;
        int RightX;

        Quaternion straight = Quaternion.Euler(new Vector3(90, 0, 0));

        bool Left=false;
        bool Right = false;
        bool Ready = false;
        bool Thrown = false;

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name == "Wall") //split for game. Maybe color javelins
            {
                reset();
            }
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.MoveRotation(straight);
            rb.MovePosition(new Vector3(0f, 5f, 0f));
        }

        void FixedUpdate()
        {
            //--------------move wrists----------------------
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
            //----------------------

            RightY = -JavControl.ball2Y;
            RightX = JavControl.ball2X;

            LeftY = -JavControl.ball1Y;
            LeftX = JavControl.ball1X;

            if (Thrown)
            {
                Debug.Log("in air");
                count = count + 1;
                if (count > 200)
                {
                    reset();
                    Debug.Log("No point");
                }
                moveThresh = 1;
                if (Right) //if right just threw
                {
                    //look for left grab
                   if (IsBetween(L_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                   IsBetween(L_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        rb.isKinematic = true;
                        Right = false;
                        Thrown = false;
                        Debug.Log("L Grabbed it");
                        Left = true;
                        Debug.Log("Left: " + Left);
                        Debug.Log("Right: " + Right);
                        count = 0;
                    }

                }
                if (Left) //could just be else
                {
                    //look for right grab
                    if (IsBetween(R_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                        IsBetween(R_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        rb.isKinematic = true;
                        Debug.Log("R Grabbed it");
                        Left = false;
                        Thrown = false;
                        Right = true;
                        Debug.Log("Left: " + Left);
                        Debug.Log("Right: " + Right);
                        count = 0;
                    }
                }
            }
            else //thrown=false
            {
                moveThresh = -1;
                if (Left == false && Right == false) 
                {
                    //who is going to grab it?
                    if (IsBetween(L_Wrist.localPosition.z, rb.position.z + 1.25, rb.position.z - 1.25) &&
                    IsBetween(L_Wrist.localPosition.y, rb.position.y + .5, rb.position.y - .5))
                    {
                        Left = true;
                        Debug.Log("L First");
                    }

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
                    if (rb.position.z < -10)
                    {                         
                        Ready = true;
                        rb.GetComponent<Renderer>().material = redMat;
                    }
                    if (Ready)
                    {
                        if (rb.position.z >-throwThresh)//if the jav crosses the corresponding thresh
                        {
                            Debug.Log("Throw");
                            Thrown = true;
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
                    if (rb.position.z > 10)
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
                            Thrown = true;
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
            
            Thrown = false;
            Right = false;
            Left = false;
            Debug.Log("reset");
            rb.isKinematic = true;
            GameObject stuck = Instantiate(javFab, rb.position, rb.rotation);
            stuck.GetComponent<Renderer>().material = rb.GetComponent<Renderer>().material;
            rb.GetComponent<Renderer>().material = whiteMat;
            rb.MoveRotation(straight);
            rb.MovePosition(new Vector3(0f,0f,0f));
            count=0;
            
        }

        public bool IsBetween(double testValue, double bound1, double bound2)
        {
            if (bound1 > bound2)
                return testValue >= bound2 && testValue <= bound1;
            return testValue >= bound1 && testValue <= bound2;
        }
    }
}