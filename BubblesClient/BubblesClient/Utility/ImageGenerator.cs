using System;
using System.Drawing;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using ThoughtWorks.QRCode.Codec;

namespace BubblesClient.Utility
{
    public class QRGeneratorException : Exception
    {
        public QRGeneratorException(Exception innerException) :
            base("Error generating QR Code. Ensure URL fits in given data size.", innerException)
        {
        }
    }

    /// <summary>
    /// Can be used to Generate QR Codes as XNA 2D Textures from a string and
    /// a graphics device.
    /// </summary>
    public class ImageGenerator
    {
        private const int DefaultVersion = 10;
        private const QRCodeEncoder.ERROR_CORRECTION DefaultErrorCorrection = QRCodeEncoder.ERROR_CORRECTION.M;

        /// <summary>
        /// Create a texture using the default version and ECC.
        /// </summary>
        /// <param name="graphicsDevice">Graphics Device used to create the texture</param>
        /// <param name="value">The string to encode</param>
        /// <returns>A texture with the generated QR code in it</returns>
        public static Texture2D GenerateQRCode(GraphicsDevice graphicsDevice, string value)
        {
            return GenerateQRCode(graphicsDevice, value, DefaultVersion, DefaultErrorCorrection);
        }

        /// <summary>
        /// Create a texture containing a QR code of the given string. Care 
        /// must be taken to ensure that the version and ECC level allow enough
        /// space for the string to be encoded.
        /// </summary>
        /// <param name="graphicsDevice">Graphics Device used to create the texture</param>
        /// <param name="value">The string to encode</param>
        /// <param name="version">The QR Code version to use</param>
        /// <param name="errorCorrection">The QR Core ECC mode to use</param>
        /// <returns>A texture with the generated QR code in it</returns>
        /// <exception cref="QRGeneratorException">If the string doesn't fit in the QR
        /// code; or if anything else went wrong during the encoding.</exception>
        public static Texture2D GenerateQRCode(GraphicsDevice graphicsDevice, string value, int version, QRCodeEncoder.ERROR_CORRECTION errorCorrection)
        {
            // Make the QR Code
            QRCodeEncoder encoder = new QRCodeEncoder();
            encoder.QRCodeVersion = version;
            encoder.QRCodeErrorCorrect = errorCorrection;

            try
            {
                // Encode the bitmap, then convert it to a texture
                return BitmapToTexture(encoder.Encode(value), graphicsDevice);
            }
            catch (Exception ex)
            {
                throw new QRGeneratorException(ex);
            }
        }

        /// <summary>
        /// Generate a texture from a web image. Downloads the image, creates a
        /// bitmap from it, then converts that to a texture.
        /// </summary>
        /// <param name="graphicsDevice">Graphics Device used to create the texture</param>
        /// <param name="URL">The URL to grab the image from</param>
        /// <returns>A texture containing the given image</returns>
        public static Texture2D GenerateFromWeb(GraphicsDevice graphicsDevice, string URL)
        {
            if (URL == null || URL == string.Empty)
            {
                URL = "http://www2.macs.hw.ac.uk/~ad133/pussy.jpg";
            }
            // Create the web request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";

            // Grab the response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Create the bitmap
            Bitmap bitmap = new Bitmap(response.GetResponseStream());

            // Remember to close this!
            response.Close();

            // Return the bitmap as a texture
            return BitmapToTexture(bitmap, graphicsDevice);
        }

        private static Texture2D BitmapToTexture(Bitmap bitmap, GraphicsDevice graphicsDevice)
        {
            using (MemoryStream s = new MemoryStream())
            {
                bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                s.Seek(0, SeekOrigin.Begin);
                return Texture2D.FromStream(graphicsDevice, s);
            }
        }
    }
}