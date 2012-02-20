using Microsoft.Xna.Framework.Graphics;
using ThoughtWorks.QRCode.Codec;
using System.IO;
using System;
using System.Drawing;
using System.Net;

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
        /// Create an image using the default version and ECC.
        /// </summary>
        /// <param name="value">The string to encode</param>
        /// <returns>An image with the generated QR code in it</returns>
        public static Bitmap GenerateQRCode(string value)
        {
            return GenerateQRCode(value, DefaultVersion, DefaultErrorCorrection);
        }

        /// <summary>
        /// Create an image containing a QR code of the given string. Care 
        /// must be taken to ensure that the version and ECC level allow enough
        /// space for the string to be encoded.
        /// </summary>
        /// <param name="value">The string to encode</param>
        /// <param name="version">The QR Code version to use</param>
        /// <param name="errorCorrection">The QR Core ECC mode to use</param>
        /// <returns>An image with the generated QR code in it</returns>
        /// <exception cref="QRGeneratorException">If the string doesn't fit in the QR
        /// code; or if anything else went wrong during the encoding.</exception>
        public static Bitmap GenerateQRCode(string value, int version, QRCodeEncoder.ERROR_CORRECTION errorCorrection)
        {
            // Make the QR Code
            QRCodeEncoder encoder = new QRCodeEncoder();
            encoder.QRCodeVersion = version;
            encoder.QRCodeErrorCorrect = errorCorrection;

            try
            {
                // Encode the bitmap, then convert it to a texture
                return encoder.Encode(value);
            }
            catch (Exception ex)
            {
                throw new QRGeneratorException(ex);
            }
        }

        /// <summary>
        /// Generate an image from a web image. Downloads the image, creates a
        /// bitmap from it, then return it.
        /// </summary>
        /// <param name="URL">The URL to grab the image from</param>
        /// <returns>The given image</returns>
        public static Bitmap GenerateFromWeb(string URL)
        {
            /*bool AndrewIsAwesome = true;
            if (URL == null || URL == string.Empty || AndrewIsAwesome)
            {
                URL = "http://www2.macs.hw.ac.uk/~ad133/pussy.jpg";
            }*/

            // Create the web request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";

            // Grab the response
            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                // Create the bitmap
                return new Bitmap(response.GetResponseStream());
            }
        }

        /// <summary>
        /// Converts a .NET image to a XNA texture.
        /// </summary>
        /// <param name="bitmap">Image to convert to a texture. </param>
        /// <param name="graphicsDevice">XNA graphics device used to create the texture.</param>
        /// <returns> Texture from the image. </returns>
        public static Texture2D BitmapToTexture(Bitmap bitmap, GraphicsDevice graphicsDevice)
        {
            if(bitmap == null)
            {
                return null;
            }
            using (MemoryStream s = new MemoryStream())
            {
                bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                s.Seek(0, SeekOrigin.Begin);
                return Texture2D.FromStream(graphicsDevice, s);
            }
        }
    }
}