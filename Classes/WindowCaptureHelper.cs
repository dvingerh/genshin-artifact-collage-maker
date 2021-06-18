using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Drawing.Drawing2D;

namespace genshin_impact_artifact_collage_maker
{
    // Taken and modified from https://stackoverflow.com/a/53252044
    public class WindowCaptureHelper
    {
        [DllImport("User32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, out POINT point);

        [DllImport("User32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, out WindowRect lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr handle);

        [DllImport("User32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        public static Bitmap ProcessWindowScreenshot(IntPtr mainHandle)
        {
            try
            {
                Rectangle genshinWindow = GetWindowClientRectangle(mainHandle);
                var bitmap = new Bitmap(genshinWindow.Width, genshinWindow.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = InterpolationMode.High;
                    g.CopyFromScreen(genshinWindow.Location, Point.Empty, genshinWindow.Size, CopyPixelOperation.SourceCopy);
                    return bitmap;
                };
            }
            catch
            {
                return null;
            }
        }

        public static Rectangle GetWindowClientRectangle(IntPtr handle)
        {
            GetClientRect(handle, out WindowRect rect);
            ClientToScreen(handle, out POINT point);
            return rect.ToRectangleOffset(point);
        }

        public struct WindowRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangleOffset(POINT p) => Rectangle.FromLTRB(p.x, p.y, Right + p.x, Bottom + p.y);
        }

        public struct POINT
        {
            public int x;
            public int y;
        }
    }
}