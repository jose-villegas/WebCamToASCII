using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using AviFile;

namespace Camera
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection usbCams;
        private VideoCaptureDevice selectedUsbCam;
        private Size dimensions;
        private BitmapToASCII bta;
        private bool viewOriginal;
        private bool viewASCII;
        private bool firstFrame = true;

        // Sistema de grabacion
        private bool grabando = false;
        private bool inicializado = false;
        private string destVideo = "";
        private VideoStream aviStream;
        private AviManager aviManager;
        Bitmap firstImage;

        public Form1()
        {
            InitializeComponent();
            bta = new BitmapToASCII();

            // Se inicializan los botones
            grabando = false;
            stopButton.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            // Buscamos e inicializamos el dispositivo de captura de video
            try
            {
                usbCams = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (usbCams.Count == 0)
                {
                    throw new ApplicationException();
                }

                foreach (FilterInfo camera in usbCams)
                {
                    deviceList.Items.Add(camera.Name);
                }

                deviceList.SelectedIndex = 0;
            }

            catch (ApplicationException)
            {
                deviceList.Items.Add("No tienes ningun dispositivo capturador de video");
            }

            viewOriginal = true;
            viewASCII = false;
        }


        private void startButton_Click(object sender, EventArgs e)
        {
            // Se reactivan los botones
            startButton.Enabled = false;
            stopButton.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            grabando = false;

            if (usbCams.Count > 0 && selectedUsbCam == null)
            {
                selectedUsbCam = new VideoCaptureDevice(usbCams[deviceList.SelectedIndex].MonikerString);
                selectedUsbCam.NewFrame += new NewFrameEventHandler(video_NewFrameEvent);
                pictureBox1.Size = selectedUsbCam.VideoCapabilities.First().FrameSize;
                dimensions = selectedUsbCam.VideoCapabilities.First().FrameSize; ;
                selectedUsbCam.Start();
            }
        }

        private void video_NewFrameEvent(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = null;
            Graphics g = null;
            Brush c = null;
            Bitmap currentFrame = (Bitmap)eventArgs.Frame.Clone();

            if (firstFrame)
            {
                bta.StoreFirstFrameParams(currentFrame);
                firstFrame = false;
            }

            if (viewASCII && viewOriginal)
            {
                bmp = currentFrame;
                g = Graphics.FromImage(currentFrame);
                c = new SolidBrush(Color.White);
            }

            else if (viewASCII && !viewOriginal)
            {
                bmp = new Bitmap(dimensions.Width, dimensions.Height);
                g = Graphics.FromImage(bmp);
                c = new TextureBrush(currentFrame);
            }

            else if (!viewASCII && viewOriginal)
            {
                bmp = currentFrame;
            }

            if (viewASCII)
            {
                String A = bta.BitmapToASCIIText(currentFrame);
                g.DrawString(A, new Font("Consolas", 7.0f), c, new System.Drawing.Point(0, 0));
            }

            // Se despliega la imagen en la ventana
            pictureBox1.Image = bmp;

            // Se guarda la primera imagen
            firstImage =(Bitmap)pictureBox1.Image.Clone();

 

            // Se guarda la imagen en el video
            if (grabando && inicializado)
            {
                aviStream.AddFrame(firstImage);
                firstImage.Dispose();
            }

        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // Se termina la grabacion si estaba encendida
            if (grabando)
            {
                grabando = false;
                terminarGrabacion();
            }

            // Se apagan los botones
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            stopButton.Enabled = false;
            startButton.Enabled = true;

            if (selectedUsbCam != null)
            {
                selectedUsbCam.SignalToStop();
                selectedUsbCam = null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopButton_Click(null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            viewOriginal = !viewOriginal;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            viewASCII = !viewASCII;
        }

        private void button3_Click(object sender, EventArgs e)
        {
           // grabando = !grabando;

           /* if (grabando)
            {*/
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
                saveFileDialog1.Filter = "Imagen (*.jpg)|*.jpg|All Files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                // Se obtiene la direccion donde se grabara el video y se inicializan las estructuras
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    destVideo = saveFileDialog1.FileName;

                    // Se salva la imagen
                    firstImage.Save(destVideo);
                }


                /* aviManager = new AviManager("prueba.avi", false);
                 aviStream = aviManager.AddVideoStream(false, 30, firstImage);
                 firstImage.Dispose();*/
           /* }
            else
            {
                terminarGrabacion();
            }*/
        }

        private void terminarGrabacion()
        {
            aviStream.Close();
            aviManager.Close();

            inicializado = false;
            grabando = false;
            Console.WriteLine("SERVICIO TERMINADO");
        }


    }
}
