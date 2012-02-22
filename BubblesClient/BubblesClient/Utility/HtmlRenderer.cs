using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
//using Microsoft.Xna.Framework.Content;
using BubblesClient.Model;

namespace BubblesClient.Utility
{
    /// <summary>
    /// Creates images from a balloon's content, using HTML, to be displayed on screen.
    /// </summary>
    public class HtmlRenderer
    {
        private Size maxCaptionBoxSize;
        private Size maxContentBoxSize;
        private PixelFormat pixFormat;
        private ImageFormat imgFormat;
        private Color maskColour;
        private Dictionary<string, string> templates;
        private Dictionary<string, Image> staticImages;

        public HtmlRenderer()
        {
            this.maxCaptionBoxSize = new Size(400, 600);
            this.maxContentBoxSize = new Size(1060, 700);
            this.pixFormat = PixelFormat.Format32bppArgb;
            this.imgFormat = ImageFormat.Png;
            this.maskColour = Color.FromArgb(0xff, 0xfa, 0xaf, 0xbe); // pink
            this.templates = new Dictionary<string, string>();
            this.staticImages = new Dictionary<string, Image>();
        }

        public void LoadTemplate(string path, string name)
        {
            string file = Path.Combine(path, Path.Combine("Html", name));
            templates[name] = File.ReadAllText(file);
        }

        public void LoadImage(string path, string name)
        {
            string file = Path.Combine(path, Path.Combine("Images", name));
            staticImages[name] = Image.FromFile(file);
        }

        private string FillTemplate(string templateText, Dictionary<string, string> vals)
        {
            string text = templateText;
            foreach(KeyValuePair<string, string> pair in vals)
            {
                text = text.Replace(pair.Key, pair.Value);
            }
            return text;
        }

        public Bitmap RenderCaption(ClientBalloon balloon)
        {
            // replace template parameters by their values
            string content = (balloon.Label == null) ? "" : balloon.Label;
            var vals = new Dictionary<string, string>();
            vals.Add("@@CONTENT@@", content);
            string html = FillTemplate(templates["caption_box.html"], vals);

            var images = new Dictionary<string, Image>(staticImages);
            return RenderHtml(maxCaptionBoxSize, html, images);
        }

        public Bitmap RenderContent(ClientBalloon balloon)
        {
            // prepare the images
            BalloonContentCache cacheEntry = balloon.BalloonContentCache;
            var images = new Dictionary<string, Image>(staticImages);
            Bitmap qrImg = null, webImg = null;
            lock (cacheEntry)
            {
                // clone images to avoid threading issues (e.g. concurrent calls to RenderContent)
                qrImg = cacheEntry[CacheType.QRCode];
                webImg = cacheEntry[CacheType.WebImage];
                if (qrImg != null)
                {
                    qrImg = (Bitmap)qrImg.Clone();
                    images["qr.png"] = qrImg;
                }
                if (webImg != null)
                {
                    webImg = (Bitmap)webImg.Clone();
                    images["web.png"] = webImg;
                }
            }

            // replace template parameters by their values
            string title = (balloon.Label == null) ? "" : balloon.Label;
            string content = (balloon.Content == null) ? "" : balloon.Content;
            if (String.IsNullOrEmpty(content) && !String.IsNullOrEmpty(balloon.Url))
            {
                content = balloon.Url;
            }
            var vals = new Dictionary<string, string>();
            vals.Add("@@TITLE@@", title);
            vals.Add("@@CONTENT@@", content);
            vals.Add("@@MASK-COLOR@@", ColorTranslator.ToHtml(maskColour));
            if(balloon.Votes >= 0)
            {
                vals.Add("@@VOTES@@", balloon.Votes.ToString());
                vals.Add("@@THUMBS-CLASS@@", "thumbsUp");
                vals.Add("@@THUMBS-IMG@@", "thumbs-up.png");
            }
            else
            {
                vals.Add("@@VOTES@@", (-balloon.Votes).ToString());
                vals.Add("@@THUMBS-CLASS@@", "thumbsDown");
                vals.Add("@@THUMBS-IMG@@", "thumbs-down.png");
            }

            // Generate CSS for images
            const int imageSize = 229;
            if (qrImg != null)
            {
                vals.Add("@@QRIMG_CSS@@", String.Format("width: {0}px; width: {0}px;", imageSize));
            }
            else
            {
                vals.Add("@@QRIMG_CSS@@", "display: none;");
            }
            if(webImg != null)
            {
                if(webImg.Width > webImg.Height)
                {
                    int realSize = Math.Min(imageSize, webImg.Width);
                    vals.Add("@@WEBIMG_CSS@@", String.Format("width: {0}px;", realSize));
                }
                else
                {
                    int realSize = Math.Min(imageSize, webImg.Height);
                    vals.Add("@@WEBIMG_CSS@@", String.Format("height: {0}px;", realSize));
                }
            }
            else
            {
                vals.Add("@@WEBIMG_CSS@@", "display: none;");
            }

            string html = FillTemplate(templates["content_box.html"], vals);
            Bitmap bmp = RenderHtml(maxContentBoxSize, html, images);
            bmp.MakeTransparent(maskColour);
            return bmp;
        }

        private Bitmap RenderHtml(Size maxSize, string html, Dictionary<string, Image> images)
        {
            // encode the HTML page text to bytes
            byte[] htmlData = Encoding.UTF8.GetBytes(html);

            using(HTMLite hLite = new HTMLite())
            {
                // this callback is used to load images
                hLite.UriHandler = (uri, type) => LoadUri(uri, type, images);
                // load the HTML page data
                hLite.LoadHtmlFromMemory("content.html", htmlData);
                // set the HTML page size to the maximum size
                Size size = maxSize;
                hLite.Measure(size.Width, size.Height);
                // detect the actual size
                var bounds = hLite.GetRootElement().Select("#bounds");
                if(bounds.Count > 0)
                {
                    size = bounds[0].Bounds.Size;
                    size.Width += bounds[0].Bounds.Left * 2;
                    size.Height += bounds[0].Bounds.Top * 2;
                }
                hLite.Measure(size.Width, size.Height);

                // render the HTML page to the image
                Bitmap img = new Bitmap(size.Width, size.Height, pixFormat);
                using(Graphics g = Graphics.FromImage(img))
                {
                    g.Clear(Color.Transparent);
                    hLite.Render(g, 0, 0, img.Width, img.Height);
                }
                return img;
            }
        }

        private byte[] LoadUri(string uri, HTMLite.ResourceType type, Dictionary<string, Image> images)
        {
            Image img;
            if(images.TryGetValue(uri, out img))
            {
                // serialize the image to a stream of bytes
                MemoryStream ms = new MemoryStream();
                img.Save(ms, imgFormat);
                byte[] data = ms.ToArray();
                return data;
            }
            return null;
        }
    }
}
