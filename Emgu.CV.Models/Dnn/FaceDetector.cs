﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2021 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;

namespace Emgu.CV.Models
{
    /// <summary>
    /// Face detector using DNN
    /// </summary>
    public class FaceDetector : DisposableObject
    {
        private String _modelFolderName = "dnn_samples_face_detector_20170830";

        private Net _faceDetectorNet = null;

        private Mat _inputBlob = new Mat();

        private Mat _detection = new Mat();

        /// <summary>
        /// Download and initialize the DNN face detector
        /// </summary>
        /// <param name="onDownloadProgressChanged">Callback when download progress has been changed</param>
        /// <returns>Async task</returns>
        public async Task Init(System.Net.DownloadProgressChangedEventHandler onDownloadProgressChanged = null)
        {
            if (_faceDetectorNet == null)
            {
                FileDownloadManager manager = new FileDownloadManager();

                manager.AddFile(
                    "https://github.com/opencv/opencv_3rdparty/raw/dnn_samples_face_detector_20170830/res10_300x300_ssd_iter_140000.caffemodel",
                    _modelFolderName,
                    "2A56A11A57A4A295956B0660B4A3D76BBDCA2206C4961CEA8EFE7D95C7CB2F2D");

                manager.AddFile(
                    "https://raw.githubusercontent.com/opencv/opencv/4.0.1/samples/dnn/face_detector/deploy.prototxt",
                    _modelFolderName,
                    "F62621CAC923D6F37BD669298C428BB7EE72233B5F8C3389BB893E35EBBCF795");

                if (onDownloadProgressChanged != null)
                    manager.OnDownloadProgressChanged += onDownloadProgressChanged;
                await manager.Download();

                if (manager.AllFilesDownloaded)
                {
                    _faceDetectorNet = DnnInvoke.ReadNetFromCaffe(manager.Files[1].LocalFile, manager.Files[0].LocalFile);
                    if (Emgu.CV.Cuda.CudaInvoke.HasCuda)
                    {
                        _faceDetectorNet.SetPreferableBackend(Emgu.CV.Dnn.Backend.Cuda);
                        _faceDetectorNet.SetPreferableTarget(Emgu.CV.Dnn.Target.Cuda);
                    }
                }
            }
        }

        /// <summary>
        /// Detect faces on the image
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="fullFaceRegions">The faces where a full facial region is detected. These images can be send to facial landmark recognition for further processing.</param>
        /// <param name="partialFaceRegions">The face region of which is close to the edge of the images. Because if may not contains all the facial landmarks, it is not recommended to send these regions to facial landmark detection.</param>
        public void Detect(Mat image, List<Rectangle> fullFaceRegions, List<Rectangle> partialFaceRegions, float confidenceThreshold = 0.5f)
        {
            int imgDim = 300;
            MCvScalar meanVal = new MCvScalar(104, 177, 123);
            Size imageSize = image.Size;
            DnnInvoke.BlobFromImage(
                image,
                _inputBlob,
                1.0,
                new Size(imgDim, imgDim),
                meanVal,
                false,
                false);
            _faceDetectorNet.SetInput(_inputBlob, "data");
            _faceDetectorNet.Forward(_detection, "detection_out");

            Rectangle imageRegion = new Rectangle(Point.Empty, image.Size);

            float[,,,] values = _detection.GetData(true) as float[,,,];
            for (int i = 0; i < values.GetLength(2); i++)
            {
                float confident = values[0, 0, i, 2];

                if (confident > confidenceThreshold)
                {
                    float xLeftBottom = values[0, 0, i, 3] * imageSize.Width;
                    float yLeftBottom = values[0, 0, i, 4] * imageSize.Height;
                    float xRightTop = values[0, 0, i, 5] * imageSize.Width;
                    float yRightTop = values[0, 0, i, 6] * imageSize.Height;
                    RectangleF objectRegion = new RectangleF(
                        xLeftBottom,
                        yLeftBottom,
                        xRightTop - xLeftBottom,
                        yRightTop - yLeftBottom);
                    Rectangle faceRegion = Rectangle.Round(objectRegion);

                    if (imageRegion.Contains(faceRegion))
                        fullFaceRegions.Add(faceRegion);
                    else
                    {
                        partialFaceRegions.Add(faceRegion);
                    }
                }
            }

        }

        protected override void DisposeObject()
        {
            if (_faceDetectorNet != null)
            {
                _faceDetectorNet.Dispose();
                _faceDetectorNet = null;
            }

            if (_inputBlob != null)
            {
                _inputBlob.Dispose();
                _inputBlob = null;
            }

            if (_detection != null)
            {
                _detection.Dispose();
                _detection = null;
            }
        }
    }
}