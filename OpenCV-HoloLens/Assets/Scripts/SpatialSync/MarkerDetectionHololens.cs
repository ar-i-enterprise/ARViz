﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Unity.Preview.SpectatorView
{
    /// <summary>
    /// Manages the capture process on the HoloLens
    /// </summary>
    public class MarkerDetectionHololens : MonoBehaviour
    {
        // Delegate fires when marker with id MarkerId is found
        // markerId: the marker id
        // pos : the marker position in World-Space
        // rot : the marker rotation
        public delegate void OnMarkerDetectedDelegate(int markerId, Vector3 pos, Quaternion rot);
        public OnMarkerDetectedDelegate OnMarkerDetected;

        /// <summary>
        /// A component for capturing photos from the HoloLens webcam
        /// </summary>
        [Tooltip("A component for capturing photos from the HoloLens webcam")]
        [SerializeField]
        private CameraCaptureHololens holoLensCapture;

        /// <summary>
        /// The physical size of the marker to search for in metres
        /// </summary>
        [Tooltip("The physical size of the marker to search for in metres")]
        [SerializeField]
        private float markerSize = 0.05f;

        /// <summary>
        /// Sound played when the hololens enters capturing mode
        /// </summary>
        [Tooltip("Sound played when the hololens enters capturing mode")]
        [SerializeField]
        private AudioSource successSound;

        /// <summary>
        /// Time the user has to hold an airtap before entering capturing mode
        /// </summary>
        [Tooltip("Time the user has to hold an airtap before entering capturing mode")]
        [SerializeField]
        private float airtapTimeToCapture;

        /// <summary>
        /// Time the camera will be capturing
        /// </summary>
        [Tooltip("Time the camera will be capturing")]
        [SerializeField]
        private float captureTimeout = 30f;

        /// <summary>
        /// Timeout to automatically stop capturing photos
        /// </summary>
        private float currentCaptureTimeout;

        /// <summary>
        /// Component used to detect markers
        /// </summary>
        private MarkerDetector detector;

        /// <summary>
        /// Is the HoloLens capturing?
        /// </summary>
        private bool capturing;

        /// <summary>
        /// A component for capturing photos from the HoloLens webcam
        /// </summary>
        public CameraCaptureHololens HoloLensCapture
        {
            get { return holoLensCapture; }
            set { holoLensCapture = value; }
        }

        /// <summary>
        /// The physical size of the marker to search for in metres
        /// </summary>
        public float MarkerSize
        {
            get { return markerSize; }
            set { markerSize = value; }
        }

        /// <summary>
        /// Sound played when the hololens enters capturing mode
        /// </summary>
        public AudioSource SuccessSound
        {
            get { return successSound; }
            set { successSound = value; }
        }

        /// <summary>
        /// Time the user has to hold an airtap before entering capturing mode
        /// </summary>
        public float AirtapTimeToCapture
        {
            get { return airtapTimeToCapture; }
            set { airtapTimeToCapture = value; }
        }

        /// <summary>
        /// Time the camera will be capturing
        /// </summary>
        public float CaptureTimeout
        {
            get { return captureTimeout; }
            set { captureTimeout = value; }
        }

        private void Start ()
        {
            //Debug.Log("################### Starting!");
#if WINDOWS_UWP
            //Debug.Log("################### Starting UWP!");
            try
            {
                OpenCVUtils.CheckOpenCVWrapperHasLoaded();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                gameObject.SetActive(false);
                return;
            }

            detector = new MarkerDetector();
            detector.Initialize();

            holoLensCapture.OnFrameCapture += ProcessImage;

            StartCapture();
#endif
        }

        private void Update()
        {
            //Debug.Log("################### Updating!");
            if (!capturing || currentCaptureTimeout <= 0)
            {
                //Debug.Log("################### Update -> not capturing!");
                return;
            }

            currentCaptureTimeout -= Time.deltaTime;
            //Debug.Log("################### currentCaptureTimeout = "+currentCaptureTimeout);

            if (currentCaptureTimeout <= 0 && capturing)
            {
                Debug.Log("Capture timed out");
                SuccessSound.Play();
                StopCapture();
            }
        }

        /// <summary>
        /// Starts capturing photos
        /// </summary>
        public void StartCapture()
        {
            currentCaptureTimeout = CaptureTimeout;
#if WINDOWS_UWP
            if(!capturing)
            {
                holoLensCapture.StartCapture();
                OnMarkerDetected += HologramOnARuco;
            }
#else
            Debug.LogWarning("Capturing is not supported on this platform");
#endif
            capturing = true;
        }

        /// <summary>
        /// Restarts the timeout to keep alive the capturing process
        /// </summary>
        public void KeepAliveCapture()
        {
            currentCaptureTimeout = CaptureTimeout;
        }

        /// <summary>
        /// Stops capturing photos
        /// </summary>
        public void StopCapture()
        {
            //Debug.Log("################### Stoping!");
            capturing = false;
            currentCaptureTimeout = 0;

#if WINDOWS_UWP
            holoLensCapture.StopCapture();
            OnMarkerDetected -= HologramOnARuco;
#else
            Debug.LogWarning("Capturing is not supported on this platform");
#endif
        }

        /// <summary>
        /// Process the image to figure out whether a marker has been detected
        /// If it has, it'll notify it
        /// </summary>
        /// <param name="imageData">The captured image</param>
        /// <param name="imageWidth">Width of the image</param>
        /// <param name="imageHeight">Height of the image</param>
        private void ProcessImage(List<byte> imageData, int imageWidth, int imageHeight)
        {
#if WINDOWS_UWP
            //Debug.Log("################### ProcessImage!");
            detector.Detect(imageData, imageWidth, imageHeight, markerSize);
            Vector3 pos;
            Quaternion rot;
            int[] detectedMarkerIds;
            detector.GetMarkerIds(out detectedMarkerIds);

            for(int i=0; i<detectedMarkerIds.Length; i++)
            {
                if(!detector.GetMarkerPose(detectedMarkerIds[i], out pos, out rot))
                {
                    Debug.Log("Can't resolve marker position for marker id: " + detectedMarkerIds[i]);
                    continue;
                }
                else
                {
                    //Debug.Log("################### ProcessImage -> if 111!");
                    Debug.Log("################### ProcessImage -> detectedMarkerIds "+detectedMarkerIds[i]);
                    Debug.Log("################### ProcessImage -> pos "+pos.ToString());
                    Debug.Log("################### ProcessImage -> rot "+rot.ToString());
                    SuccessSound.Play();
                    HologramOnARuco(detectedMarkerIds[i], pos, rot);
                    /*
                    if(OnMarkerDetected != null)
                    {
                        Debug.Log("################### ProcessImage -> if 222!");
                        OnMarkerDetected(detectedMarkerIds[i], pos, rot);
                        Debug.Log("################### ProcessImage -> OnMarkerDetected -> pos "+pos);
                        Debug.Log("################### ProcessImage -> OnMarkerDetected -> rot "+rot);
                    }
                    */
                    StopCapture();
                }
            }
#endif
        }

        private void HologramOnARuco(int markerId, Vector3 pos, Quaternion rot)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            cube.transform.localScale = Vector3.one * markerSize;
            // sphere.transform.position = pos;
        }

        private void OnDestroy()
        {
#if WINDOWS_UWP
            detector.Terminate();
            holoLensCapture.OnFrameCapture -= ProcessImage;
#endif
        }
    }
}
