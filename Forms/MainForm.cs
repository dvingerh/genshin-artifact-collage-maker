using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gi_artifact_capture
{
    public partial class MainForm : Form
    {
        string savePrefix;
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
            Bitmap gameScreenshot = null;
            Bitmap croppedGameScreenshot = null;
            for (int progressIndex = 0; progressIndex <= 4; progressIndex++)
            {
                await Task.Delay((int)TimeOutUpDown.Value);
                gameScreenshot = WindowCaptureHelper.ProcessWindowScreenshot(genshinHandle);
                if (gameScreenshot == null)
                {
                    Invoke((MethodInvoker)delegate
                    {
                        Activate();
                        MessageBox.Show("Could not capture the game window. Ensure it's not minimalized or unfocused and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    progressIndex = -1;
                }
                else
                {
                    int gameScreenWidth = gameScreenshot.Width;
                    int cropWidth = Convert.ToInt32(0.275 * gameScreenshot.Width);
                    int cropHeight = Convert.ToInt32(0.90 * gameScreenshot.Height);
                    int offsetWidth = gameScreenWidth - cropWidth;

                    croppedGameScreenshot = CropGameImage(gameScreenshot, cropWidth, cropHeight, offsetWidth);
                }

                if (SaveInOwnDirectoryCheckBox.Checked)
                    Directory.CreateDirectory(savePrefix);

                switch (progressIndex)
                {
                    case 0:
                        FlowerBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            if (SaveInOwnDirectoryCheckBox.Checked)
                                croppedGameScreenshot.Save(Path.Combine(savePrefix, $"{savePrefix}_flower.png"));
                            else
                                croppedGameScreenshot.Save($"{savePrefix}_flower.png");
                        break;
                    case 1:
                        PlumeBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            if (SaveInOwnDirectoryCheckBox.Checked)
                                croppedGameScreenshot.Save(Path.Combine(savePrefix, $"{savePrefix}_plume.png"));
                            else
                                croppedGameScreenshot.Save($"{savePrefix}_plume.png");
                        break;
                    case 2:
                        SandsBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            if (SaveInOwnDirectoryCheckBox.Checked)
                                croppedGameScreenshot.Save(Path.Combine(savePrefix, $"{savePrefix}_sands.png"));
                            else
                                croppedGameScreenshot.Save($"{savePrefix}_sands.png");
                        break;
                    case 3:
                        GobletBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            if (SaveInOwnDirectoryCheckBox.Checked)
                                croppedGameScreenshot.Save(Path.Combine(savePrefix, $"{savePrefix}_goblet.png"));
                            else
                                croppedGameScreenshot.Save($"{savePrefix}_goblet.png");
                        break;
                    case 4:
                        CircletBitmap = croppedGameScreenshot;
                        if (SaveIndividualImagesCheckBox.Checked)
                            if (SaveInOwnDirectoryCheckBox.Checked)
                                croppedGameScreenshot.Save(Path.Combine(savePrefix, $"{savePrefix}_circlet.png"));
                            else
                                croppedGameScreenshot.Save($"{savePrefix}_circlet.png");
                        break;
                    default:
                        Invoke((MethodInvoker)delegate
                        {
                            StartButton.Enabled = true;
                            StartButton.Text = "Start";
                            CaptureProgressBar.Value = 0;
                        });
                        break;
                }
                if (progressIndex == -1)
                    break;

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
                    if (SaveInOwnDirectoryCheckBox.Checked)
                        Process.Start(Path.Combine(savePrefix, $"{savePrefix}.png"));
                    else
                        Process.Start($"{savePrefix}.png");
                }
            }
        }

        private void DoBeeps(bool last = false)
        {
            Console.Beep(2000, 100);
            Console.Beep(2000, 100);
            if (last)
            {
                Console.Beep(2000, 100);
                Console.Beep(2000, 100);
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
            
            savePrefix = $"artifacts_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
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

        private void OpenSaveLocationButtonClick(object sender, EventArgs e)
        {
            Process.Start(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
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
                newWidth -= borderSize;

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
            if (SaveInOwnDirectoryCheckBox.Checked)
                collageImage.Save(Path.Combine(savePrefix, $"{savePrefix}.png"));
            else
                collageImage.Save($"{savePrefix}.png");
        }
    }
}