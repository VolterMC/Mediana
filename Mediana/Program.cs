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
        // Replace 2560x1440 with your screen resolution, 
        // but don't touch the 20x12 one
        static Bitmap BigScreen = new Bitmap(2560, 1440);
        static Bitmap SmallScreen = new Bitmap(1, 1);

        // Can't touch this
        static RGBSurface Surface;
        static bool RunUpdateThread;
        static Stopwatch Stopwatch;
        static Stopwatch FpsMeter;
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
            FpsMeter = new Stopwatch();
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
            System.Drawing.Color pixel = SmallScreen.GetPixel(0, 0);

            // Scale the color so that one of the base colors value is always 255
            byte max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            double scale = 255.0 / max;
            RGB.NET.Core.Color color = new RGB.NET.Core.Color((byte)(scale * pixel.R), (byte)(scale * pixel.G), (byte)(scale * pixel.B));

            // Assign the color to all the LEDs in your ledGroup
            ledGroup.Brush = new SolidColorBrush(color);         
        }

        static void UpdateLeds()
        {
            int cnt = 0;
            while (RunUpdateThread == true)
            {
                FpsMeter.Restart();
                Stopwatch.Restart();
                SetLedColors(CorsairGroup);
                Surface.Update();
                Stopwatch.Stop();
                if (1000 / FPS_LIMIT - Stopwatch.ElapsedMilliseconds > 0)
                {
                    Thread.Sleep(1000 / FPS_LIMIT - (int)Stopwatch.ElapsedMilliseconds);
                }
                FpsMeter.Stop();

                if (++cnt == 10)
                {
                    cnt = 0;
                    double ActualFps = 1000.0 / FpsMeter.ElapsedMilliseconds;
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("FPS limit: " + FPS_LIMIT + ", Actual FPS: " + ActualFps.ToString("n2"));
                }
            }
        }
    }
}