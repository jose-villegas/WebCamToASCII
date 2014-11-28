using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Camera {
    class BitmapToASCII {
        private Size dimensions;
        private Thread[] worker;
        private int stride;
        private int bytesPerPixel;
        private int totalLength;
        private byte[] rgbValues;
        private int verticalSize;
        private int horizontalSize;
        private char[] ASCII_Scale;
        private char[] ASCII_Bitmap;
        private int totalCharacters;

        public BitmapToASCII()
        {
            verticalSize = 10;
            horizontalSize = 5;
            // Creamos la escala de equivalencia para el [Promedio Lumninancia (0-255)] - [Caracter]
            String ASCII_Scale_String = @"$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\|()1{}[]?-_+~<>i!lI;:,""^`'. """;
            ASCII_Scale = new char[255];

            for (int i = 0; i < 255; i = i + 1) {
                int equivalentCharacter = (int)Math.Floor(((double)i / 255.0) * 70.0);
                ASCII_Scale[i] = ASCII_Scale_String.ElementAt(equivalentCharacter);
            }
        }

        public String BitmapToASCIIText(Bitmap bmp)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, dimensions.Width, dimensions.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            try {
                String a;
                IntPtr ptr = bmData.Scan0;
                // Copiamos los valores RGB en el arreglo.
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, totalLength);

                for (int i = 0; i < worker.GetLength(0); i++) {
                    int j = i;
                    worker[i] = new Thread(() => StoreEquivalentCharacter(j));
                }

                for (int i = 0; i < worker.GetLength(0); i++) {
                    worker[i].Start();
                }

                for (int i = 0; i < worker.GetLength(0); i++) {
                    worker[i].Join();
                }

                a = new String(ASCII_Bitmap);
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, totalLength);
                return new String(ASCII_Bitmap);
            }

            finally {
                bmp.UnlockBits(bmData);
            }
        }

        public void StoreEquivalentCharacter(int y)
        {
            double sumGray = 0;
            int pos = 0;
            int indexChar = ((dimensions.Width / horizontalSize) - 1) * y;

            if (y > 0) {
                indexChar += y - 1;
            }

            for (int x = 0; x < dimensions.Width - horizontalSize; x = x + horizontalSize) {
                // Calculamos el promedio de luminancia en un rectangulo del tamaño del caracter
                for (int i = 0; i < verticalSize; i++) {
                    for (int j = 0; j < horizontalSize; j++) {
                        pos = ((i + y * 10) * dimensions.Width + (j + x)) * bytesPerPixel;
                        sumGray += (0.21f * rgbValues[pos] + 0.71f * rgbValues[pos + 1] + 0.07f * rgbValues[pos + 1]);
                    }
                }

                ASCII_Bitmap[indexChar] = ASCII_Scale[(int)sumGray / (horizontalSize * verticalSize)];
                sumGray = 0;
                indexChar++;
            }

            ASCII_Bitmap[indexChar] = '\n';
        }

        public void StoreFirstFrameParams(Bitmap bmp)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            try {
                dimensions = new Size(bmp.Width, bmp.Height);
                stride = bmData.Stride;
                bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bmp.PixelFormat) / 8;
                totalLength = Math.Abs(stride) * dimensions.Height;
                rgbValues = new byte[totalLength];
                totalCharacters = ((dimensions.Height / verticalSize) - 1) * ((dimensions.Width / horizontalSize) - 1) +
                                  ((dimensions.Height / verticalSize) - 1);
                ASCII_Bitmap = new char[totalCharacters];
                // Un hilo por cada linea de texto
                worker = new Thread[(dimensions.Height / verticalSize) - 1];
            }

            finally {
                bmp.UnlockBits(bmData);
            }
        }
    }
}