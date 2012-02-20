using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Html;
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
        private string htmlTemplate;
        private List<GCHandle> bufferList;

        public ContentRenderer()
        {
            this.boxSize = new Size(1060, 630);
            this.pixFormat = PixelFormat.Format32bppArgb;
            this.imgFormat = ImageFormat.Png;
            this.maskColour = Color.FromArgb(0xff, 0xfa, 0xaf, 0xbe); // pink
            this.bufferList = new List<GCHandle>();
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

            // prepare the images
            var images = new Dictionary<string, Image>();
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
                RenderUsingHTMLite(bmp, html, images);
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

        private void RenderUsingDrawingHtml(Bitmap img, string html, Dictionary<string, Image> images)
        {
            using(Graphics g = Graphics.FromImage(img))
            {
                g.Clear(Color.White);
                HtmlRenderer.Render(g, html,
                    new RectangleF(0, 0, boxSize.Width, boxSize.Height), true);
            }
        }

        private void RenderUsingHTMLite(Bitmap img, string html, Dictionary<string, Image> images)
        {
            IntPtr hLite = HTMLiteCreateInstance();
            if(hLite == IntPtr.Zero)
            {
                return;
            }

            // encode the HTML page text to bytes
            byte[] htmlData = Encoding.UTF8.GetBytes(html);

            // this callback is used to load images
            HTMLCallback callback = (handle, pMsg) => HTMLiteCallback(handle, pMsg, images);
            // keep the callback object alive until the library is released
            GCHandle cbHandle = GCHandle.Alloc(callback, GCHandleType.Normal);
            try
            {
                ThrowOnError(HTMLiteSetCallback(hLite, callback));
                // load the HTML page data
                ThrowOnError(HTMLiteLoadHtmlFromMemory(hLite, "content.html", htmlData, (uint)htmlData.Length));
                // set the HTML page size
                ThrowOnError(HTMLiteMeasure(hLite, img.Width, img.Height));

                try
                {
                    // render the HTML page to the image
                    using(Graphics g = Graphics.FromImage(img))
                    {
                        g.Clear(Color.Transparent);
                        IntPtr hDc = g.GetHdc();
                        ThrowOnError(HTMLiteRender(hLite, hDc, 0, 0, img.Width, img.Height));
                        g.ReleaseHdc(hDc);
                    }
                }
                finally
                {
                    ThrowOnError(HTMLiteDestroyInstance(hLite));
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

        private uint HTMLiteCallback(IntPtr hLite, IntPtr pMsg, Dictionary<string, Image> images)
        {
            uint code = (uint)Marshal.ReadInt32(pMsg, 8);
            switch(code)
            {
            case HLN_LOAD_DATA:
                NMHL_LOAD_DATA loadData = (NMHL_LOAD_DATA)Marshal.PtrToStructure(pMsg, typeof(NMHL_LOAD_DATA));
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
                    ThrowOnError(HTMLiteSetDataReady(hLite, loadData.uri, hData, (uint)data.Length));
                }
                break;
            }
            return 0;
        }

        public enum HtmlResult : int
        {
            // HLDOM_OK_NOT_HANDLED
            DOM_Success_NotHandled = -1,
            // HLDOM_OK
            DOM_Success = 0,
            // HLDOM_INVALID_HWND
            DOM_Failure_InvalidHWND = 1,
            // HLDOM_INVALID_HANDLE
            DOM_Failure_InvalidHandle = 2,
            // HLDOM_PASSIVE_HANDLE
            DOM_Failure_PassiveHandle = 3,
            // HLDOM_INVALID_PARAMETER
            DOM_Failure_InvalidParameter = 4,
            // HLDOM_OPERATION_FAILED
            DOM_Failure_OperationFailed = 5,
            DOM_Failure_Unsupported = 256,

            // HPR_OK
            Print_Success = 0,
            // HPR_INVALID_HANDLE
            Print_Failure_InvalidHandle = 1,
            // HPR_INVALID_FORMAT
            Print_Failure_InvalidFormat = 2,
            // HPR_FILE_NOT_FOUND
            Print_Failure_FileNotFound = 3,
            // HPR_INVALID_PARAMETER
            Print_Failure_InvalidParameter = 4,
            // HPR_INVALID_STATE
            Print_Failure_InvalidState = 5,

            Value_Success = 0,
            // HV_BAD_PARAMETER
            Value_Bad_Parameter = 1,
            // HV_INCOMPATIBLE_TYPE
            Value_Incompatible_Type = 2
        }

        public enum HtmlResourceType : int
        {
            Unknown = -1,
            // HLRT_DATA_HTML
            Html = 0,
            // HLRT_DATA_IMAGE
            Image = 1,
            // HLRT_DATA_STYLE
            StyleSheet = 2,
            // HLRT_DATA_CURSOR
            Cursor = 3,
            // HLRT_DATA_SCRIPT
            Script = 4
        };

        private void ThrowOnError(uint result)
        {
            if(result != 0)
            {
                throw new Exception(MessageFromCode(result));
            }
        }

        private string MessageFromCode(uint errorCode)
        {
            switch((HtmlResult)errorCode)
            {
            case HtmlResult.Print_Success:
                return "No error.";
            case HtmlResult.Print_Failure_InvalidHandle:
                return "Invalid handle.";
            case HtmlResult.Print_Failure_InvalidFormat:
                return "Invalid format.";
            case HtmlResult.Print_Failure_FileNotFound:
                return "File not found.";
            case HtmlResult.Print_Failure_InvalidParameter:
                return "Invalid parameter.";
            case HtmlResult.Print_Failure_InvalidState:
                return "Invalid state.";
            default:
                return String.Format("Unknown error code {0}.", errorCode);
            }
        }

        #region PInvoke stuff
        const uint HLN_LOAD_DATA = 0xAFF + 0x02;

        struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public uint code;
        };

        struct NMHL_LOAD_DATA
        {
            public NMHDR hdr;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string uri;
            public IntPtr outData;
            public uint outDataSize;
            public uint dataType;
        }

        private delegate uint HTMLCallback(IntPtr hLite, IntPtr pMsg);

        const string HTMLitePath = @"Content\Html\htmlayout.dll";

        [DllImport(HTMLitePath)]
        extern static IntPtr HTMLiteCreateInstance();

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteDestroyInstance(IntPtr hLite);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteLoadHtmlFromMemory(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string baseURI,
            byte[] data, uint dataSize);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteLoadHtmlFromFile(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteMeasure(IntPtr hLite, int width, int height);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteRender(IntPtr hLite, IntPtr hDc, int x, int y, int sx, int sy);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteRenderOnBitmap(IntPtr hLite, IntPtr hBmp, int x, int y, int sx, int sy);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteSetDataReady(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string url,
            IntPtr data, uint dataSize);

        [DllImport(HTMLitePath)]
        extern static uint HTMLiteSetCallback(IntPtr hLite, HTMLCallback cb);
        #endregion
    }
}
