using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gi_artifact_capture
{
    public partial class MainForm : Form
    {
        string saveFileName;
        Process genshinProc;
        IntPtr genshinHandle;

        private Bitmap FlowerBitmap, PlumeBitmap, SandsBitmap, GobletBitmap, CircletBitmap;

        private enum ProcessDPIAwareness
        {
            ProcessDPIUnaware = 0,
            ProcessSystemDPIAware = 1,
            ProcessPerMonitorDPIAware = 2
        }

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);

        private static void SetDpiAwareness()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                    SetProcessDpiAwareness(ProcessDPIAwareness.ProcessPerMonitorDPIAware);
            }
            catch (EntryPointNotFoundException) { }
        }

        public MainForm()
        {
            SetDpiAwareness();
            InitializeComponent();
            UpdateGameRunningStatus(null, null);
        }

        private async Task DoCaptureGameScreen()
        {
            Bitmap gameScreenshot;
            Bitmap croppedGameScreenshot;
            for(int progressIndex = 0; progressIndex <= 4; progressIndex++)
            {
                await Task.Delay((int)TimeOutUpDown.Value);
                gameScreenshot = WindowCaptureHelper.ProcessWindowScreenshot(genshinHandle);
                int gameScreenWidth = gameScreenshot.Width;
                int cropWidth = Convert.ToInt32(0.275 * gameScreenshot.Width);
                int cropHeight = Convert.ToInt32(0.90 * gameScreenshot.Height);
                int offsetWidth = gameScreenWidth - cropWidth;

                croppedGameScreenshot = CropGameImage(gameScreenshot, cropWidth, cropHeight, offsetWidth);

                switch (progressIndex)
                {
                    case 0:
                        FlowerBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            croppedGameScreenshot.Save($"{saveFileName}_flower.png");
                     break;
                    case 1:
                        PlumeBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            croppedGameScreenshot.Save($"{saveFileName}_plume.png");
                        break;
                    case 2:
                        SandsBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            croppedGameScreenshot.Save($"{saveFileName}_sands.png");
                        break;
                    case 3:
                        GobletBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            croppedGameScreenshot.Save($"{saveFileName}_goblet.png");
                        break;
                    case 4:
                        CircletBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            croppedGameScreenshot.Save($"{saveFileName}_circlet.png");
                        break;
                }
                Invoke((MethodInvoker)delegate
                {
                    CaptureProgressBar.PerformStep();
                });

                DoBeeps(last: progressIndex == 4);
                if (progressIndex == 4)
                {
                    CreateArtifactCompilation();
                    Invoke((MethodInvoker)delegate
                    {
                        Activate();
                        StartButton.Enabled = true;
                        StartButton.Text = "Start";
                    });
                    Process.Start($"{saveFileName}.png");
                }
            }
        }

        private void DoBeeps(bool last = false)
        {
            Console.Beep(5000, 100);
            Console.Beep(5000, 100);
            if (last)
            {
                Console.Beep(5000, 100);
                Console.Beep(5000, 100);
            }
        }

        private async void StartCapture(object sender, System.EventArgs e)
        {
            GenshinProcessTimer.Stop();

            FlowerBitmap = null;
            PlumeBitmap = null;
            SandsBitmap = null;
            GobletBitmap = null;
            CircletBitmap = null;
            
            saveFileName = $"artifacts_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            CaptureProgressBar.Value = 0;
            StartButton.Enabled = false;
            StartButton.Text = "Capturing...";

            FocusGameWindow();
            await Task.Run(async() => await DoCaptureGameScreen());
        }

        private void FocusGameWindow()
        {
            if (WindowCaptureHelper.IsIconic(genshinHandle))
                WindowCaptureHelper.ShowWindow(genshinHandle, 9);

            WindowCaptureHelper.SetForegroundWindow(genshinHandle);
            WindowCaptureHelper.SwitchToThisWindow(genshinHandle, true);
        }


        private Bitmap CropGameImage(Bitmap originalImage, int cropWidth, int cropHeight, int offsetWidth)
        {
            Bitmap croppedImage = new Bitmap(cropWidth, cropHeight);
            Rectangle cropBounds = new Rectangle(offsetWidth, 0, cropWidth, cropHeight);

            using (Graphics g = Graphics.FromImage(croppedImage))
                g.DrawImage(originalImage, 0, 0, cropBounds, GraphicsUnit.Pixel);

            return croppedImage;
        }

        private void UpdateGameRunningStatus(object sender, EventArgs e)
        {
            genshinProc = Process.GetProcessesByName("genshinimpact").FirstOrDefault();
            if (genshinProc != null)
            {
                genshinHandle = genshinProc.MainWindowHandle;
                StartButton.Enabled = true;
                StartButton.Text = "Start";
            }
            else
            {
                StartButton.Enabled = false;
                StartButton.Text = "Game not running";
            }
        }

        // Taken from https://stackoverflow.com/a/56035786
        private Bitmap DrawArtifactImageBorder(Bitmap bmp, int borderSize = 10, bool skipRightBorder = false)
        {
            int newWidth = bmp.Width + (borderSize * 2);
            int newHeight = bmp.Height + (borderSize * 2);

            if (skipRightBorder)
                newWidth -= 10;

            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gfx = Graphics.FromImage(newImage))
            {
                using (Brush border = new SolidBrush(Color.Black))
                    gfx.FillRectangle(border, 0, 0, newWidth, newHeight);
                gfx.DrawImage(bmp, new Rectangle(borderSize, borderSize, bmp.Width, bmp.Height));
            }
            return newImage;
        }

        private Bitmap MergeArtifactImages(List<Bitmap> images)
        {
            int totalWidth = 0;
            int totalHeight = 0;

            for (int i = 0; i < images.Count; i++)
            {
                images[i] = DrawArtifactImageBorder(images[i], borderSize: 5, skipRightBorder: i != 4);
                totalWidth += images[i].Width;
                totalHeight = images[i].Height > totalHeight ? images[i].Height : totalHeight;
            }

            Bitmap collageBitmap = new Bitmap(totalWidth, totalHeight);
            using (Graphics g = Graphics.FromImage(collageBitmap))
            {
                int currentWidth = 0;
                foreach (Bitmap image in images)
                {
                    g.DrawImage(image, currentWidth, 0);
                    currentWidth += image.Width;
                }
            }
            return collageBitmap;
        }

        private void CreateArtifactCompilation()
        {
            List<Bitmap> artifactImages = new List<Bitmap>
            {
                FlowerBitmap,
                PlumeBitmap,
                SandsBitmap,
                GobletBitmap,
                CircletBitmap
            };
            Bitmap collageImage = MergeArtifactImages(artifactImages);
            collageImage.Save($"{saveFileName}.png");
        }
    }
}