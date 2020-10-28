#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking Based on Color Example
    /// Referring to https://www.youtube.com/watch?v=hQ-bpfdWQh8.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class Pink_track : MonoBehaviour
    {
        int start = 798;

        public JavRB2P Jav2P;

        private Vector3 Displacement;

        /// The texture.
        Texture2D texture;
        const int MAX_NUM_OBJECTS = 2;
        const int MIN_OBJECT_AREA = 35 * 35;

        Scalar redHSVmin;
        Scalar redHSVmax;
        Scalar greenHSVmin;
        Scalar greenHSVmax;

        public int ball1Y;
        public int ball1X;

        public int ball2Y;
        public int ball2X;

        int rows = 50; //make public, control size of squares
        int cols = 50;

        float PThrowThresh;
        float MThrowThresh;

        Mat rgbMat;
        Mat thresholdMat;
        Mat hsvMat;

        ColorObject red = new ColorObject("red");
        ColorObject green = new ColorObject("green");
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

            redHSVmax = red.getHSVmax();
            redHSVmin = red.getHSVmin();
            greenHSVmax = green.getHSVmax();
            greenHSVmin = green.getHSVmin();
        }



        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                PThrowThresh=((float)Jav2P.throwThresh+13.3f)*33f;
                MThrowThresh = (-(float)Jav2P.throwThresh + 13.3f) * 33f;

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, redHSVmin, redHSVmax, thresholdMat);
                morphOps(thresholdMat);
                trackFilteredObject(red, thresholdMat, hsvMat, rgbMat);


                if (Input.GetKeyDown(KeyCode.G))
                {
                    greenHSVmax = findHSVmax(hsvMat,start);
                    greenHSVmin = findHSVmin(hsvMat,start);
                }

                Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange (hsvMat, greenHSVmin, greenHSVmax, thresholdMat);
                morphOps (thresholdMat);
                trackFilteredObject (green, thresholdMat, hsvMat, rgbMat);


                Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rows, cols), new Scalar(255, 0, 0), 2);
                Imgproc.rectangle(rgbMat, new Point(PThrowThresh, 0), new Point(MThrowThresh, 480), new Scalar(255, 0, 0), 2);
                Imgproc.rectangle(rgbMat, new Point(start, 0), new Point(rows+ start, cols), new Scalar(0, 0, 255), 2);
                Utils.fastMatToTexture2D(rgbMat, texture);


                if (Input.GetKeyDown(KeyCode.P))
                {
                    redHSVmax = findHSVmax(hsvMat,0);
                    redHSVmin = findHSVmin(hsvMat,0);
                }

               
            }
        }
        private Scalar findHSVmax(Mat hsv,int start) //need to create subset mat in target area
        {
            double[] max = hsv.get(0, start); //for comparison
            Debug.Log(max[0]);
            double[] check;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    check = hsv.get(i, j + start);
                    if (check[0] > max[0]) { max[0] = check[0]; }
                    if (check[1] > max[1]) { max[1] = check[1]; }
                    if (check[2] > max[2]) { max[2] = check[2]; }
                }
            }

            Scalar maxHSV = new Scalar(max[0], max[1], max[2]);
            Debug.Log(maxHSV);
            return maxHSV;
        }

        private Scalar findHSVmin(Mat hsv,int start) //need to create subset mat in target area
        {
            double[] min = hsv.get(0, start);
            double[] check;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    check = hsv.get(i, j + start);
                    if (check[0] < min[0]) { min[0] = check[0]; }
                    if (check[1] < min[1]) { min[1] = check[1]; }
                    if (check[2] < min[2]) { min[2] = check[2]; }
                }
            }

            Scalar minHSV = new Scalar(min[0], min[1], min[2]);
            Debug.Log(minHSV);
            return minHSV;
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
                        //draw object location on screen
                        //drawObject(colorObjects, cameraFeed, temp, contours, hierarchy);
                        if (theColorObject == red)
                        {
                            ball1X = (colorObjects[0].getXPos() - 400);
                            ball1Y = (colorObjects[0].getYPos() - 250);
                        }
                        if(theColorObject == green)
                        {
                            ball2X = (colorObjects[0].getXPos() - 400);
                            ball2Y = (colorObjects[0].getYPos() - 250);
                        } 
                    }
                }
                else
                {
                    Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
            }
        }
        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
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
        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }
    }
}

#endif