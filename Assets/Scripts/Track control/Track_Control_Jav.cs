#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Specialized;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OpenCVForUnityExample
{
    //[RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class Track_Control_Jav : MonoBehaviour
    {
       
        /// The texture.
        Texture2D texture;
        const int MAX_NUM_OBJECTS = 2;
        const int MIN_OBJECT_AREA = 5 * 5;
        //public JavRB Jav;
        public int ball1Y;
        public int ball1X;
        int last1X;
        int last1Y;

        int rows = 50; //make public, control size of squares
        int cols = 50;


        //float ThrowThresh;
        //float hitThresh;


        Mat rgbMat;
        Mat thresholdMat;
        Mat hsvMat;

        Scalar redHSVmin;
        Scalar redHSVmax;

        ColorObject red = new ColorObject("red");
        //ColorObject green = new ColorObject("green");
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        FpsMonitor fpsMonitor;
        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        

        void Update()
        {
            //ThrowThresh=((float)Jav.thrownThresh+13.3f)*33f;
            //hitThresh = ((float)Jav.hitThresh + 13.3f) * 33f;

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();
                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

                if (Input.GetKeyDown(KeyCode.P))
                {
                    redHSVmax = findHSVmax(hsvMat, rows, cols);
                    redHSVmin = findHSVmin(hsvMat, rows, cols);
                }

                Core.inRange(hsvMat, red.getHSVmin(), red.getHSVmax(), thresholdMat);
                morphOps(thresholdMat);
                trackFilteredObject(red, thresholdMat, hsvMat, rgbMat);
                //Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rows, cols), new Scalar(0, 200, 0), 2);
                //Imgproc.rectangle(rgbMat, new Point(ThrowThresh, 480), new Point(hitThresh, 0), new Scalar(200, 0, 0), 2);
                Utils.fastMatToTexture2D(rgbMat, texture);

            }
        }
        private Scalar findHSVmin(Mat hsv, int row, int col) //need to create subset mat in target area
        {
            double[] min = hsv.get(0, 0);
            double[] check;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    check = hsv.get(i, j);
                    if (check[0] < min[0]) { min[0] = check[0]; }
                    if (check[1] < min[1]) { min[1] = check[1]; }
                    if (check[2] < min[2]) { min[2] = check[2]; }
                }
            }

            Scalar minHSV = new Scalar(min[0], min[1], min[2]);
            Debug.Log(minHSV);
            return minHSV;
        }
        private Scalar findHSVmax(Mat hsv, int row, int col) //need to create subset mat in target area
        {
            double[] max = hsv.get(0, 0); //for comparison
            double[] check;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {//row and col --> W and H as starting point
                    check = hsv.get(i, j);//get(i+W,j+H) where W and H are the starting location

                    if (check[0] > max[0]) { max[0] = check[0]; }
                    if (check[1] > max[1]) { max[1] = check[1]; }
                    if (check[2] > max[2]) { max[2] = check[2]; }
                }
            }

            Scalar maxHSV = new Scalar(max[0], max[1], max[2]);
            Debug.Log(maxHSV);
            return maxHSV;
        }

        /// <param name="thresh">Thresh.</param>
        private void morphOps(Mat thresh)
        {
            //create structuring element that will be used to "dilate" and "erode" image.
            //the element chosen here is a 3px by 3px rectangle
            Mat erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
            //dilate with larger element so make sure object is nicely visible
            Mat dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(8, 8));

            Imgproc.erode(thresh, thresh, erodeElement);
            Imgproc.erode(thresh, thresh, erodeElement);

            Imgproc.dilate(thresh, thresh, dilateElement);
            Imgproc.dilate(thresh, thresh, dilateElement);
        }

        /// <param name="theColorObject">The color object.</param>
        /// <param name="threshold">Threshold.</param>
        /// <param name="HSV">HS.</param>
        /// <param name="cameraFeed">Camera feed.</param>
        private void trackFilteredObject(ColorObject theColorObject, Mat threshold, Mat HSV, Mat cameraFeed)
        {
            List<ColorObject> colorObjects = new List<ColorObject>();
            Mat temp = new Mat();
            threshold.copyTo(temp);
            //these two vectors needed for output of findContours
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            //find contours of filtered image using openCV findContours function
            Imgproc.findContours(temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);

            //use moments method to find our filtered object
            bool colorObjectFound = false;
            if (hierarchy.rows() > 0)
            {
                int numObjects = hierarchy.rows();
                //if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
                if (numObjects < MAX_NUM_OBJECTS)
                {
                    for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0])
                    {

                        Moments moment = Imgproc.moments(contours[index]);
                        double area = moment.get_m00();

                        //if the area is less than 20 px by 20px then it is probably just noise
                        //if the area is the same as the 3/2 of the image size, probably just a bad filter
                        //we only want the object with the largest area so we safe a reference area each
                        //iteration and compare it to the area in the next iteration.
                        if (area > MIN_OBJECT_AREA)
                        {
                            ColorObject colorObject = new ColorObject();
                            colorObject.setXPos((int)(moment.get_m10() / area));
                            colorObject.setYPos((int)(moment.get_m01() / area));
                            colorObject.setType(theColorObject.getType());
                            colorObject.setColor(theColorObject.getColor());
                            colorObjects.Add(colorObject);
                            colorObjectFound = true;
                        }
                        else
                        {
                            colorObjectFound = false;
                        }
                    }
                    //let user know you found an object
                    if (colorObjectFound == true)
                    {
                        if (theColorObject == red)
                        {
                            ball1X = (colorObjects[0].getXPos() - 375);
                            ball1Y = (colorObjects[0].getYPos() - 240);
                            if (last1X==0 || last1Y == 0) //just for first frame
                            {
                                Debug.Log("first time through");
                            }
                            else if(Mathf.Abs(last1X - ball1X) > 150 || Mathf.Abs(last1Y - ball1Y) > 150)
                            {
                                ball1X = last1X;
                                ball1Y = last1Y;
                                Debug.Log("Prevented jump");
                            }


                            Debug.Log(Mathf.Abs(last1X - ball1X));
                            
                            last1X = ball1X;
                            last1Y = ball1Y;
                        }
                    }
                }
                else
                {
                    Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
            }
        }

        public void OnWebCamTextureToMatHelperInitialized()
        {
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGB24, false);
            Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            //Debug.Log("W: " + (float)Screen.width + "  H: " + (float)Screen.height);

            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
            rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            thresholdMat = new Mat();
            hsvMat = new Mat();
        }

        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();
            if (thresholdMat != null)
                thresholdMat.Dispose();
            if (hsvMat != null)
                hsvMat.Dispose();
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
        }
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }
    }
}

#endif