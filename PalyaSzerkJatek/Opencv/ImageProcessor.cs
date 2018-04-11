﻿using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PalyaSzerkJatek
{
    class ImageProcessor
    {
        public static readonly string LOAD_IMG = "img";
        public static readonly string LOAD_FROM_CAMERA = "cam";
        private OpenCvSharp.Point[][] contours ;
        private Thread camera;
        VideoCapture cap;
        public delegate void CaptureEventHandler(Mat frame, Mat Thresholded, List<Wall> walls, List<Gem> gems);
        //public delegate void CaptureEventHandler(Mat frame, IEnumerable<Wall> walls,IEnumerable<Gem> gems);
        public event CaptureEventHandler CaptureChanged;
        private Mat thresholded = new Mat();

         
        private int thresholdNum = 120;

        public int ThresholdNum
        {
            get { return thresholdNum ; }
            set
            {
                if( value > 0 && value < 255 )
                thresholdNum = value;
            }
        }
        private List<Wall> wallObjects = new List<Wall>();
        private List<Gem> gemObjects = new List<Gem>();

        public void capture(string mode)
        {
            Mat frame = new Mat();
                switch (mode)
            {
                case "img":
                    
                frame = Cv2.ImRead("C:\\Users\\barth\\Pictures\\palya.jpg");
                    if (frame.Empty())
                        throw new NullReferenceException("Nem sikerült a képet betölteni");
                    break;
                case "cam":
                   
                    cap = new VideoCapture();
                    cap.Open(1);
                    cap.Set(CaptureProperty.FrameWidth, 1920);
                    cap.Set(CaptureProperty.FrameHeight, 1080);
                    break;
            }
            if(cap != null)
            while (true)
            {
            bool success = cap.Read(frame);
            if (success)
            {
            thresholded = process(frame);
            
            frame = findPoly(frame);
            CaptureChanged(frame,thresholded,wallObjects, gemObjects);
            wallObjects.Clear();
            gemObjects.Clear();
            }
            
            }
            CaptureChanged(frame, process(frame),wallObjects, gemObjects);


        }

        private Mat findPoly(Mat frame)
        {
            foreach (OpenCvSharp.Point[] contour in contours)
            {
                //var lines = Cv2.HoughLines(frame, 0.4, 1, 120);
                
                OpenCvSharp.Point[] walls = Cv2.ApproxPolyDP(contour, 5.2, true);
                if (Cv2.ContourArea(walls, false) > 10000 && walls.Length < 25) {
                    drawShape(frame, walls, Scalar.Green);
                    //setLabel(frame, $"{Cv2.ContourArea(walls,false) / 1000 }", walls);

                    wallObjects.AddRange( Converter.ContourToWall( walls ) );
                        //Converter.ContourToWall(walls) );
                    
                }
                OpenCvSharp.Point[] shapes = Cv2.ApproxPolyDP(contour, 6.2, true);
                if (Cv2.ContourArea(shapes, false) < 10000 && Cv2.ContourArea(shapes, false) >  500 && shapes.Length <= 5 && shapes.Length > 2) {
                
                    List<double> cos = new List<double>();
                    for (int j = 2; j < shapes.Length + 1; j++)
                        cos.Add(Angle(shapes[j % shapes.Length], shapes[j - 2], shapes[j - 1]));
                
                    // Sort ascending the cosine values
                
                
                    cos.Sort();
                
                    // Get the lowest and the highest cosine
                    double mincos = cos.First();
                    double maxcos = cos.Last();
                    if (maxcos > 0.8 || -mincos > 0.8)
                    {
                    drawShape(frame, shapes, Scalar.GreenYellow);
                    wallObjects.AddRange( Converter.ContourToWall( shapes ) );

                    }
                    else
                    {
                    Gem gemObject = Converter.ContourToGem(shapes);
                    if( !gemObjects.Any( g => Math.Abs(g.Position.X - gemObject.Position.X) < 10 && Math.Abs(g.Position.Y - gemObject.Position.Y) < 10))
                    gemObjects.Add(gemObject);
                    drawShape(frame, shapes, Scalar.Blue);
                    }
                
                    //if(Cv2.ContourArea(shapes, false) < 3000)
                }
            
            }
            //Cv2.DrawContours(frame, contours, -1,Scalar.Red);
            return frame;
        }

        public void startCapture(string mode)
        {
            camera = new Thread(() => capture(mode));
            camera.Start();
        }
        public void stopCapture()
        {
            camera.Abort();
        }

        private Mat process(Mat frame)
        {
            Mat grayScaled = new Mat();
           // Mat thresholded = new Mat();
            
            Cv2.CvtColor(frame, grayScaled, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(grayScaled, thresholded, thresholdNum, 255, ThresholdTypes.Binary);
            //Cv2.Erode(thresholded, thresholded,10, new OpenCvSharp.Point(-1, -1), 2);
            //Cv2.Dilate(thresholded, thresholded, new Mat(), new OpenCvSharp.Point(-1, -1), 2);
           // Cv2.Blur(thresholded, thresholded, new Size(20, 20));
           // Cv2.Threshold(thresholded, thresholded, 240, 255, ThresholdTypes.Binary);
            findContours(thresholded,frame);

            return thresholded; // findContours(thresholded,frame); ;

        }

        public Mat findContours(Mat thresholded,Mat frame)
        {
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresholded,out contours,out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);
           
            Mat ret = new Mat();
           // frame.CopyTo(ret);
            //foreach (Point[] contour in contours)
           // {
            //   Point[] appr = Cv2.ApproxPolyDP(contour,10, true);
          //      if(Cv2.ContourArea(appr,false) >1 && appr.Length < 10)
           //    drawShape(ret, appr, Scalar.Red);
          //  }
            return ret;
        }

        private void drawShape(Mat img, OpenCvSharp.Point[] approx, Scalar color)
        {
            int count = approx.Length;
            for (int i = 0; i < count; i++)
            {
                 if (i != count - 1)
                    Cv2.Line(img, approx[i].X, approx[i].Y, approx[i + 1].X, approx[i + 1].Y, color, 3);
                  else Cv2.Line(img, approx[i].X, approx[i].Y, approx[0].X, approx[0].Y, color, 3);
                Cv2.Circle(img, approx[i], 4, Scalar.Red,3);


            }

        }
        private void setLabel(Mat im, string label, OpenCvSharp.Point[] contour)
        {
            var fontface = HersheyFonts.HersheySimplex; // HersheyFonts.HersheyScriptSimplex;
            double scale = 1.5;
        int thickness = 1;
        int baseline = 0;

            Size text = Cv2.GetTextSize(label, fontface, scale, thickness, out baseline);
            Rect r = Cv2.BoundingRect(contour);
            
            OpenCvSharp.Point pt =  new OpenCvSharp.Point(r.X + ((r.Width - text.Width) / 2), r.Y + ((r.Height + text.Height) / 2));
            //Cv2.Rectangle(im, pt + new OpenCvSharp.Point(0, baseline), pt + new OpenCvSharp.Point(text.Width, text.Height), new Scalar(255,255,255));
            Cv2.PutText(im, label, pt, fontface, scale,new  Scalar(0, 0, 0), thickness, LineTypes.Link8);
        }
        static double Angle(OpenCvSharp.Point pt1, OpenCvSharp.Point pt2, OpenCvSharp.Point pt0)
        {
            double dx1 = pt1.X - pt0.X;
            double dy1 = pt1.Y - pt0.Y;
            double dx2 = pt2.X - pt0.X;
            double dy2 = pt2.Y - pt0.Y;
            return (dx1 * dx2 + dy1 * dy2) / Math.Sqrt((dx1 * dx1 + dy1 * dy1) * (dx2 * dx2 + dy2 * dy2) + 1e-10);
        }
    }
}