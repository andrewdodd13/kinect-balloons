using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Html;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BubblesClient.Model;

namespace BubblesClient.Utility
{
    /// <summary>
    /// Creates textures from a balloon's content, to be displayed on screen.
    /// </summary>
    public class ContentRenderer
    {
        private Size boxSize;
        private PixelFormat pixFormat;
        private ImageFormat imgFormat;
        private string htmlTemplate;

        public ContentRenderer()
        {
            this.boxSize = new Size(1027, 722);
            this.pixFormat = PixelFormat.Format32bppArgb;
            this.imgFormat = ImageFormat.Png;
        }

        public void LoadTemplate(ContentManager manager)
        {
            string path = Path.Combine(manager.RootDirectory, "Html/test_html.html");
            htmlTemplate = File.ReadAllText(path);
        }

        public Texture2D Render(GraphicsDevice device, ClientBalloon balloon)
        {
            if(device == null)
            {
                return null;
            }
            string title = (balloon.Label == null) ? "" : balloon.Label;
            string content = (balloon.Content == null) ? "" : balloon.Content;
            string html = htmlTemplate.Replace("@@TITLE@@", title);
            html = html.Replace("@@CONTENT@@", content);

            using(Bitmap bmp = new Bitmap(boxSize.Width, boxSize.Height, pixFormat))
            using(Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                HtmlRenderer.Render(g, html,
                    new RectangleF(0, 0, boxSize.Width, boxSize.Height), true);
                using(MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, imgFormat);
                    return Texture2D.FromStream(device, ms);
                }
            }
        }
    }
}
