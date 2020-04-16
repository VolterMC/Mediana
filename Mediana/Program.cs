using RGB.NET.Brushes;
using RGB.NET.Core;
using RGB.NET.Devices.Corsair;
using RGB.NET.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace Mediana
{
    class Program
    {
        // Replace 2560x1440 with your screen resolution
        static Bitmap BigScreen = new Bitmap(2560, 1440);
        static Bitmap SmallScreen = new Bitmap(1, 1);

        // Can't touch this
        static RGBSurface Surface;
        static bool RunUpdateThread;
        static Stopwatch Stopwatch;
        static Stopwatch ActualFpsMeter;
        static Stopwatch PotentialFpsMeter;
        static ListLedGroup CorsairGroup;

        // You might want to lower this to hard limit the FPS, but increasing it won't give you anything
        static int FPS_LIMIT = 30;

        static void Main(string[] args)
        {
            Surface = RGBSurface.Instance;
            // You should set some device filters here
            Surface.LoadDevices(CorsairDeviceProvider.Instance);
            Surface.AlignDevices();

            foreach (IRGBDevice device in Surface.GetDevices<IRGBDevice>())
            {
                Console.WriteLine("Found " + device.DeviceInfo.DeviceName);
            }
            CorsairGroup = new ListLedGroup(Surface.Leds);

            Thread UpdateThread = new Thread(UpdateLeds);
            RunUpdateThread = true;
            Stopwatch = new Stopwatch();
            ActualFpsMeter = new Stopwatch();
            PotentialFpsMeter = new Stopwatch(); 
            UpdateThread.Start();
            Console.WriteLine("Running Mediana. Press any key or close this window to exit.\n\n");
            Console.ReadKey();
            RunUpdateThread = false;
        }

        static void SetLedColors(ILedGroup ledGroup)
        {
            using (Graphics g = Graphics.FromImage(BigScreen))
            {
                // Take screenshot
                g.CopyFromScreen(0, 0, 0, 0, BigScreen.Size);
            }

            using (Graphics g = Graphics.FromImage(SmallScreen))
            {
                // Squeeze the screenshot into a 1x1 bitmap
                g.DrawImage(BigScreen, 0, 0, 1, 1);
            }

            // Get the color of the squeezed pixel
            System.Drawing.Color pixelRGB = SmallScreen.GetPixel(0, 0);

            // Convert RGB -> HSV
            HSV pixelHSV = new HSV (pixelRGB);

            // Crank up saturation
            //if (pixelHSV.H > 0.1)
            //    pixelHSV.S = 1.0;

            // Convert HSV -> RGB
            System.Drawing.Color newPixelRGB = pixelHSV.ToRGB();
            
            RGB.NET.Core.Color color = new RGB.NET.Core.Color((newPixelRGB.R), (newPixelRGB.G), (newPixelRGB.B));

            // Assign the color to all the LEDs in your ledGroup
            ledGroup.Brush = new SolidColorBrush(color);         
        }

        static void UpdateLeds()
        {
            int cnt = 0;
            while (RunUpdateThread == true)
            {
                ActualFpsMeter.Restart();
                PotentialFpsMeter.Restart();
                SetLedColors(CorsairGroup);
                Surface.Update();
                PotentialFpsMeter.Stop();
                if (1000 / FPS_LIMIT - PotentialFpsMeter.ElapsedMilliseconds > 0)
                {
                    Thread.Sleep(1000 / FPS_LIMIT - (int)PotentialFpsMeter.ElapsedMilliseconds);
                }
                ActualFpsMeter.Stop();

                if (++cnt == 10)
                {
                    cnt = 0;
                    double ActualFps = 1000.0 / ActualFpsMeter.ElapsedMilliseconds;
                    double PotentialFps = 1000.0 / PotentialFpsMeter.ElapsedMilliseconds;
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("FPS limit: " + FPS_LIMIT + ", Actual FPS: " + ActualFps.ToString("n2") + ", Potential FPS: " + PotentialFps.ToString("n2"));
                }
            }
        }
    }

    class HSV
    {
        public double H, S, V;

        public HSV (double H, double S, double V)
        {
            this.H = H;
            this.S = S;
            this.V = V;
        }

        public HSV (System.Drawing.Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double max = Math.Max(Math.Max(r, g), b);
            double min = Math.Min(Math.Min(r, g), b);
            double delta = max - min;

            V = max;

            if (delta < 0.000000001)
            {
                H = 0;
                S = 0;
                return;
            }

            if (max > 0.0)
                S = delta / max;
            else
            {
                H = 0;
                S = 0;
                return;
            }

            if (r == max)
                H = (g - b) / delta;
            else if (g == max)
                H = 2.0 + (b - r) / delta;
            else
                H = 4.0 + (r - g) / delta;

            H *= 60.0;
            H += H < 0.0 ? 360.0 : 0.0;
        }

        public System.Drawing.Color ToRGB ()
        {
            if (S == 0.0)
            {
                return System.Drawing.Color.FromArgb((byte)(255*V), (byte)(255 * V), (byte)(255 * V));
            }

            double h = H;
            h = h > 360.0 ? 0.0 : h;
            h /= 60.0;
            byte hInt = (byte)Math.Floor(h);
            double hDec = h - hInt;
            double x = V * (1.0 - S);
            double y = V * (1.0 - (S * hDec));
            double z = V * (1.0 - (S * (1.0 - hDec)));

            switch (hInt)
            {
                case 0:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * V), (byte)(255.0 * z), (byte)(255.0 * x));
                case 1:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * y), (byte)(255.0 * V), (byte)(255.0 * x));
                case 2:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * x), (byte)(255.0 * V), (byte)(255.0 * z));
                case 3:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * x), (byte)(255.0 * y), (byte)(255.0 * V));
                case 4:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * z), (byte)(255.0 * x), (byte)(255.0 * V));
                case 5:
                    return System.Drawing.Color.FromArgb((byte)(255.0 * V), (byte)(255.0 * x), (byte)(255.0 * y));
                default:
                    return System.Drawing.Color.Black;
            }

        }
    }
}