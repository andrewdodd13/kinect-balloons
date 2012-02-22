using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Content;
using BubblesClient.Model;

namespace BubblesClient.Utility
{
    /// <summary>
    /// Creates textures from a balloon's content, to be displayed on screen.
    /// </summary>
    public class ContentRenderer
    {
        private Size maxCaptionBoxSize;
        private Size maxContentBoxSize;
        private PixelFormat pixFormat;
        private ImageFormat imgFormat;
        private Color maskColour;
        private Dictionary<string, string> templates;
        private Dictionary<string, Image> staticImages;

        public ContentRenderer()
        {
            this.maxCaptionBoxSize = new Size(400, 600);
            this.maxContentBoxSize = new Size(1060, 700);
            this.pixFormat = PixelFormat.Format32bppArgb;
            this.imgFormat = ImageFormat.Png;
            this.maskColour = Color.FromArgb(0xff, 0xfa, 0xaf, 0xbe); // pink
            this.templates = new Dictionary<string, string>();
            this.staticImages = new Dictionary<string, Image>();
        }

        public void LoadContent(ContentManager manager)
        {
            LoadTemplate(manager.RootDirectory, "caption_box.html");
            LoadTemplate(manager.RootDirectory, "content_box.html");
            LoadImage(manager.RootDirectory, "thumbs-up.png");
            LoadImage(manager.RootDirectory, "thumbs-down.png");
        }

        private void LoadTemplate(string path, string name)
        {
            string file = Path.Combine(path, Path.Combine("Html", name));
            templates[name] = File.ReadAllText(file);
        }

        private void LoadImage(string path, string name)
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
            Stopwatch w = new Stopwatch();
            w.Start();
            Bitmap bmp = RenderHtml(maxCaptionBoxSize, html, images);
            w.Stop();
            Trace.WriteLine(String.Format("Caption box rendered in: {0} s", w.Elapsed.TotalSeconds));
            return bmp;
        }

        public Bitmap RenderContent(ClientBalloon balloon)
        {
            // prepare the images
            var images = new Dictionary<string, Image>(staticImages);
            if(!String.IsNullOrWhiteSpace(balloon.Url))
            {
                images["qr.png"] = ImageGenerator.GenerateQRCode(balloon.Url);
            }
            if(!String.IsNullOrWhiteSpace(balloon.ImageUrl))
            {
                images["web.png"] = ImageGenerator.GenerateFromWeb(balloon.ImageUrl);
            }

            // replace template parameters by their values
            string title = (balloon.Label == null) ? "" : balloon.Label;
            string content = (balloon.Content == null) ? "" : balloon.Content;
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

            Image webImg = null;
            const int webImageSize = 229;
            if(images.TryGetValue("web.png", out webImg))
            {
                if(webImg.Width > webImg.Height)
                {
                    int realSize = Math.Min(webImageSize, webImg.Width);
                    vals.Add("@@WEBIMG_CSS@@", String.Format("width: {0}px;", realSize));
                }
                else
                {
                    int realSize = Math.Min(webImageSize, webImg.Height);
                    vals.Add("@@WEBIMG_CSS@@", String.Format("height: {0}px;", realSize));
                }
            }
            else
            {
                vals.Add("@@WEBIMG_CSS@@", "display: none;");
            }
            string html = FillTemplate(templates["content_box.html"], vals);

            Stopwatch w = new Stopwatch();
            w.Start();
            Bitmap bmp = RenderHtml(maxContentBoxSize, html, images);
            bmp.MakeTransparent(maskColour);
            w.Stop();
            Trace.WriteLine(String.Format("Content box rendered in: {0} s", w.Elapsed.TotalSeconds));
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
