using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
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
        private Color maskColour;
        private Dictionary<string, string> templates;
        private Dictionary<string, Image> staticImages;
        private List<GCHandle> bufferList;

        public ContentRenderer()
        {
            this.boxSize = new Size(1060, 650);
            this.pixFormat = PixelFormat.Format32bppArgb;
            this.imgFormat = ImageFormat.Png;
            this.maskColour = Color.FromArgb(0xff, 0xfa, 0xaf, 0xbe); // pink
            this.templates = new Dictionary<string, string>();
            this.staticImages = new Dictionary<string, Image>();
            this.bufferList = new List<GCHandle>();
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

        public Texture2D RenderContent(GraphicsDevice device, ClientBalloon balloon)
        {
            if(device == null)
            {
                return null;
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
            string html = FillTemplate(templates["content_box.html"], vals);

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

            using(Bitmap bmp = new Bitmap(boxSize.Width, boxSize.Height, pixFormat))
            {
                Stopwatch w = new Stopwatch();
                w.Start();
                RenderHtml(bmp, html, images);
                bmp.MakeTransparent(maskColour);
                w.Stop();
                Trace.WriteLine(String.Format("Content box rendered in: {0} s", w.Elapsed.TotalSeconds));
                
                using(MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, imgFormat);
                    return Texture2D.FromStream(device, ms);
                }
            }
        }

        private void RenderHtml(Bitmap img, string html, Dictionary<string, Image> images)
        {
            // encode the HTML page text to bytes
            byte[] htmlData = Encoding.UTF8.GetBytes(html);

            // this callback is used to load images
            HTMLite.Callback callback = (handle, pMsg) => HTMLiteCallback(handle, pMsg, images);

            // keep the callback object alive until the library is released
            GCHandle cbHandle = GCHandle.Alloc(callback, GCHandleType.Normal);
            try
            {
                using(HTMLite hLite = new HTMLite())
                {
                    hLite.SetCallback(callback);
                    // load the HTML page data
                    hLite.LoadHtmlFromMemory("content.html", htmlData);
                    // set the HTML page size
                    hLite.Measure(img.Width, img.Height);

                    // render the HTML page to the image
                    using(Graphics g = Graphics.FromImage(img))
                    {
                        g.Clear(Color.Transparent);
                        IntPtr hDc = g.GetHdc();
                        hLite.Render(hDc, 0, 0, img.Width, img.Height);
                        g.ReleaseHdc(hDc);
                    }
                }
            }
            finally
            {
                cbHandle.Free();
            }

            // un-pin the memory allocated for the images
            foreach(GCHandle pData in bufferList)
            {
                pData.Free();
            }
            bufferList.Clear();
        }

        private uint HTMLiteCallback(HTMLite hLite, IntPtr pMsg, Dictionary<string, Image> images)
        {
            HTMLite.MessageCode code = (HTMLite.MessageCode)Marshal.ReadInt32(pMsg, 8);
            switch(code)
            {
            case HTMLite.MessageCode.HLN_LOAD_DATA:
                var loadData = hLite.ReadMessage<HTMLite.NMHL_LOAD_DATA>(pMsg);
                Image img;
                if(images.TryGetValue(loadData.uri, out img))
                {
                    // serialize the image to a stream of bytes
                    MemoryStream ms = new MemoryStream();
                    img.Save(ms, imgFormat);
                    byte[] data = ms.ToArray();

                    // pass the buffer to the library
                    GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    bufferList.Add(dataHandle);
                    IntPtr hData = dataHandle.AddrOfPinnedObject();
                    hLite.SetDataReady(loadData.uri, hData, (uint)data.Length);
                }
                break;
            }
            return 0;
        }
    }
}
