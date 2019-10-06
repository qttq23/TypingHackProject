using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Interop;
using Tesseract;
using System.Threading;

namespace TrySomething
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // References
    //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys.send?view=netframework-4.8
    //https://stackoverflow.com/questions/15292175/c-sharp-using-sendkey-function-to-send-a-key-to-another-application
    //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
    //https://stackoverflow.com/questions/16598390/tesseract-ocr-simple-example

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            
        }


        




        

        private void TitleBarGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized) // In maximum window state case, window will return normal state and continue moving follow cursor
                {
                    this.WindowState = WindowState.Normal;
                    System.Windows.Application.Current.MainWindow.Top = 3;// 3 or any where you want to set window location affter return from maximum state
                }
                this.DragMove();
            }
        }




        bool Stop = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Stop = true;
            this.Close();
        }

        const int Space = 5;
        private void XPlusButton_Click(object sender, RoutedEventArgs e)
        {
            this.Width += Space;
        }

        private void XMinusButton_Click(object sender, RoutedEventArgs e)
        {
            this.Width -= Space;
        }

        private void YPlusButton_Click(object sender, RoutedEventArgs e)
        {
            this.Height += Space;
        }

        private void YMinusButton_Click(object sender, RoutedEventArgs e)
        {
            this.Height -= Space;
        }


        DispatcherTimer timer;
        Thread thread;
        double contentGrid_X;
        double contentGrid_Y;
        double contentGrid_Width;
        double contentGrid_Height;
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // save coordinates of 'content grid'
            System.Windows.Point locationFromScreen =
                ContentGrid.PointToScreen(new System.Windows.Point(0, 0));
            contentGrid_X = locationFromScreen.X;
            contentGrid_Y = locationFromScreen.Y;
            contentGrid_Width = ContentGrid.ActualWidth;
            contentGrid_Height = ContentGrid.ActualHeight;

            // hide window
            this.WindowState = WindowState.Minimized;

            // wait some seconds
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            
            timer.Tick += (paramSender, paramArgs) =>
            {
                timer.Stop();

                Thread t = new Thread(new ThreadStart(StartProcessing));
                Stop = false;
                t.Start();
           
            };
            timer.Start();

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop = true;
        }



	[DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);


        [DllImport("User32.dll")]
        static extern IntPtr GetForegroundWindow();
        private void StartProcessing()
        {
            // LOOP 
            while (Stop == false)
            {
                try
                {
                    // capture screen of 'content grid'
                    string filename = AppDomain.CurrentDomain.BaseDirectory + "\\temp.png";
                    CaptureScreen(
                        contentGrid_X,
                        contentGrid_Y,
                        contentGrid_Width,
                        contentGrid_Height,
                        filename);


                    // convert image to text
                    string result = String.Empty;
                    //using (Bitmap image = new Bitmap(filename))
                    //{
                    //    result = GetText(image);
                    //}
                    result = GetText(bitmap);
                    //System.Windows.MessageBox.Show(result);


                    // send result text to destination process
                    // split
                    string[] tokens = result.Split(new string[] { " " },
                                                    StringSplitOptions.RemoveEmptyEntries);


                    // send
                    IntPtr hwnd = GetForegroundWindow();
                    SetForegroundWindow(hwnd);
                    foreach (var token in tokens)
                    {

                        try{
                            SendKeys.SendWait(token);
                            Thread.Sleep(200);
                            SendKeys.SendWait( " ");

                            
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show(ex.ToString());
                        }
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }


                // END LOOP
                Thread.Sleep(200);
            }

        }

        // capture screen
        Bitmap bitmap;
        public void CaptureScreen(double x, double y, double width, double height, string destination)
        {
            int ix, iy, iw, ih;
            ix = Convert.ToInt32(x);
            iy = Convert.ToInt32(y);
            iw = Convert.ToInt32(width);
            ih = Convert.ToInt32(height);


            using (Bitmap image = new Bitmap(iw, ih,
                   System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {

                Graphics g = Graphics.FromImage(image);

                g.CopyFromScreen(ix, iy, 0, 0,
                         new System.Drawing.Size(iw, ih),
                         CopyPixelOperation.SourceCopy);

                //image.Save(destination, System.Drawing.Imaging.ImageFormat.Png);
                bitmap = new Bitmap(image);
            }
        }


	// change ocr engine here
        public string GetText(Bitmap imgsource)
        {
            var ocrtext = string.Empty;
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                using (var img = PixConverter.ToPix(imgsource))
                {

                    using (var page = engine.Process(img))
                    {
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                // for each line in the paragraph
                                var line = iter.GetText(PageIteratorLevel.TextLine);
                                line = line.Remove(line.Length - 1);
                                ocrtext += line + " ";

                            } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                        }

                        //ocrtext = page.GetText();
                    }
                }
            }

            return ocrtext;
        }

        
    }
}
