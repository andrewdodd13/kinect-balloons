using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BubblesClient.Utility
{
    /// <summary>
    /// Wraps the HTMLite library that renders HTML pages to images.
    /// </summary>
    public class HTMLite : IDisposable
    {
        /// <summary>
        /// This delegate can be used to load images and other content referenced in HTML pages.
        /// </summary>
        public delegate byte[] LoadUriHandler(string uri, ResourceType type);

        private IntPtr hLite;
        private GCHandle cbHandle;
        private List<GCHandle> bufferList;

        /// <summary>
        /// Delegate used to load images and other content referenced in HTML pages.
        /// </summary>
        public LoadUriHandler UriHandler
        {
            get;
            set;
        }

        public HTMLite()
        {
            this.hLite = HTMLiteCreateInstance();
            if(this.hLite == IntPtr.Zero)
            {
                throw new Exception("Could not load HTMLlite");
            }
            this.bufferList = new List<GCHandle>();

            // keep the callback object alive until Dispose is called
            HTMLCallback cb = Callback;
            this.cbHandle = GCHandle.Alloc(cb);
            ThrowOnError(HTMLiteSetCallback(this.hLite, cb));
        }

        ~HTMLite()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(cbHandle.IsAllocated)
            {
                if(hLite != IntPtr.Zero)
                {
                    HTMLiteSetCallback(hLite, null);
                }
                cbHandle.Free();
            }

            if(hLite != IntPtr.Zero)
            {
                HTMLiteDestroyInstance(hLite);
                hLite = IntPtr.Zero;
            }

            FreeBuffers();
        }

        /// <summary>
        /// Load an HTML page from a byte buffer.
        /// </summary>
        /// <param name="baseURI"> URI of the page. </param>
        /// <param name="data"> Buffer containing the HTML data to load. </param>
        public void LoadHtmlFromMemory(string baseURI, byte[] data)
        {
            uint result = HTMLiteLoadHtmlFromMemory(hLite, baseURI, data, (uint)data.Length);
            ThrowOnError(result);
        }

        /// <summary>
        /// Load an HTML page from a file.
        /// </summary>
        /// <param name="path"> Path of the HTML page to load. </param>
        public void LoadHtmlFromFile(string path)
        {
            uint result = HTMLiteLoadHtmlFromFile(hLite, path);
            ThrowOnError(result);
        }

        /// <summary>
        /// Set the current page's dimensions.
        /// </summary>
        /// <param name="width"> Width of the page. </param>
        /// <param name="height"> Height of the page. </param>
        public void Measure(int width, int height)
        {
            uint result = HTMLiteMeasure(hLite, width, height);
            ThrowOnError(result);
        }

        /// <summary>
        /// Render the page using the graphics context.
        /// </summary>
        /// <param name="g"> Graphics context. </param>
        /// <param name="x"> X coordinate of the top-left corner of the area to render. </param>
        /// <param name="y"> Y coordinate of the top-left corner of the area to render. </param>
        /// <param name="width"> Width of the area to render. </param>
        /// <param name="height"> Height of the area to render. </param>
        public void Render(Graphics g, int x, int y, int width, int height)
        {
            IntPtr hDc = g.GetHdc();
            try
            {
                uint result = HTMLiteRender(hLite, hDc, x, y, width, height);
                ThrowOnError(result);
            }
            finally
            {
                g.ReleaseHdc(hDc);
                FreeBuffers();
            }
        }

        public Element GetRootElement()
        {
            IntPtr root = IntPtr.Zero;
            uint result = HTMLiteGetRootElement(hLite, ref root);
            ThrowOnError(result);
            return Element.Get(root);
        }

        protected void SetDataReady(string url, byte[] data)
        {
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            bufferList.Add(dataHandle);
            uint result = HTMLiteSetDataReady(hLite, url, dataHandle.AddrOfPinnedObject(), (uint)data.Length);
            ThrowOnError(result);
        }

        protected void FreeBuffers()
        {
            // un-pin the memory allocated for the data
            foreach(GCHandle pData in bufferList)
            {
                pData.Free();
            }
            bufferList.Clear();
        }

        protected uint Callback(IntPtr hLite, IntPtr pMsg)
        {
            MessageCode code = (MessageCode)Marshal.ReadInt32(pMsg, 8);
            if((code == MessageCode.HLN_LOAD_DATA) && (UriHandler != null))
            {
                var loadData = ReadMessage<NMHL_LOAD_DATA>(pMsg);
                byte[] data = UriHandler(loadData.uri, (ResourceType)loadData.dataType);
                if(data != null)
                {
                    SetDataReady(loadData.uri, data);
                }
            }
            return 0;
        }

        protected T ReadMessage<T>(IntPtr pMsg) where T : struct
        {
            if(pMsg == IntPtr.Zero)
            {
                throw new ArgumentNullException("pMsg");
            }
            return (T)Marshal.PtrToStructure(pMsg, typeof(T));
        }

        #region Elements
        public class Element
        {
            private static Dictionary<IntPtr, Element> elements = new Dictionary<IntPtr, Element>();

            private IntPtr hEle;
            
            public List<Element> Children
            {
                get;
                set;
            }

            public Rectangle Bounds
            {
                get;
                set;
            }

            public string Tag
            {
                get;
                set;
            }

            protected Element(IntPtr hEle)
            {
                this.hEle = hEle;
                this.Tag = GetTag();
                this.Bounds = GetBounds(ELEMENT_AREAS.ROOT_RELATIVE | ELEMENT_AREAS.MARGIN_BOX);
                this.Children = GetChildren();
            }

            public List<Element> Select(string cssSelector)
            {
                List<Element> elements = new List<Element>();
                HTMLayoutElementCallback cb = delegate(IntPtr hEle, IntPtr user)
                {
                    elements.Add(Element.Get(hEle));
                };
                GCHandle cbHandle = GCHandle.Alloc(cb);
                try
                {
                    HTMLayoutSelectElements(hEle, cssSelector, cb, IntPtr.Zero);
                }
                finally
                {
                    cbHandle.Free();
                }
                return elements;
            }

            internal static Element Get(IntPtr hEle)
            {
                Element e;
                if(hEle == IntPtr.Zero)
                {
                    return null;
                }
                else if(!elements.TryGetValue(hEle, out e))
                {
                    e = new Element(hEle);
                }
                return e;
            }

            private Rectangle GetBounds(ELEMENT_AREAS areas)
            {
                RECT rect = new RECT();
                uint result = HTMLayoutGetElementLocation(hEle, ref rect, areas);
                ThrowOnError(result);
                return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            }

            private string GetTag()
            {
                string tag = null;
                uint result = HTMLayoutGetElementType(hEle, ref tag);
                ThrowOnError(result);
                return tag;
            }

            private List<Element> GetChildren()
            {
                var children = new List<Element>();
                for(uint i = 0; i < GetChildrenCount(); i++)
                {
                    children.Add(GetChild(i));
                }
                return children;
            }

            private uint GetChildrenCount()
            {
                uint count = 0;
                uint result = HTMLayoutGetChildrenCount(hEle, ref count);
                ThrowOnError(result);
                return count;
            }

            private Element GetChild(uint index)
            {
                IntPtr hChild = IntPtr.Zero;
                uint result = HTMLayoutGetNthChild(hEle, index, ref hChild);
                ThrowOnError(result);
                return Get(hChild);
            }
        }
        #endregion

        #region Enums and structs
        public enum Result : int
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

        public enum ResourceType : int
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
        }

        public enum MessageCode : uint
        {
            HLN_LOAD_DATA = 0xAFF + 0x02
        }

        protected struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public uint code;
        };

        protected struct NMHL_LOAD_DATA
        {
            public NMHDR hdr;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string uri;
            public IntPtr outData;
            public uint outDataSize;
            public uint dataType;
        }

        protected struct SIZE
        {
            public int width;
            public int height;
        }

        protected struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [Flags]
        protected enum ELEMENT_AREAS
        {
            ROOT_RELATIVE = 0x01,       // - or this flag if you want to get HTMLayout window relative coordinates,
            //   otherwise it will use nearest windowed container e.g. popup window.
            SELF_RELATIVE = 0x02,       // - "or" this flag if you want to get coordinates relative to the origin
            //   of element iself.
            CONTAINER_RELATIVE = 0x03,  // - position inside immediate container.
            VIEW_RELATIVE = 0x04,       // - position relative to view - HTMLayout window

            CONTENT_BOX = 0x00,   // content (inner)  box
            PADDING_BOX = 0x10,   // content + paddings
            BORDER_BOX = 0x20,   // content + paddings + border
            MARGIN_BOX = 0x30,   // content + paddings + border + margins 

            BACK_IMAGE_AREA = 0x40, // relative to content origin - location of background image (if it set no-repeat)
            FORE_IMAGE_AREA = 0x50, // relative to content origin - location of foreground image (if it set no-repeat)

            SCROLLABLE_AREA = 0x60,   // scroll_area - scrollable area in content box 

        };
        #endregion

        #region P/invoke stuff
        protected delegate uint HTMLCallback(IntPtr hLite, IntPtr pMsg);
        protected delegate void HTMLayoutElementCallback(IntPtr hEle, IntPtr user);

        private const string HTMLitePath = @"Content\Html\htmlayout.dll";

        [DllImport(HTMLitePath)]
        private extern static IntPtr HTMLiteCreateInstance();

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteDestroyInstance(IntPtr hLite);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteLoadHtmlFromMemory(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string baseURI,
            byte[] data, uint dataSize);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteLoadHtmlFromFile(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteMeasure(IntPtr hLite, int width, int height);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteRender(IntPtr hLite, IntPtr hDc, int x, int y, int sx, int sy);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteRenderOnBitmap(IntPtr hLite, IntPtr hBmp, int x, int y, int sx, int sy);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteSetDataReady(IntPtr hLite, [MarshalAs(UnmanagedType.LPWStr)] string url,
            IntPtr data, uint dataSize);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteSetCallback(IntPtr hLite, HTMLCallback cb);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLiteGetRootElement(IntPtr hLite, ref IntPtr he);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLayoutGetElementType(IntPtr he, [MarshalAs(UnmanagedType.LPStr)] ref string type);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLayoutGetElementLocation(IntPtr he, ref RECT location, ELEMENT_AREAS areas);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLayoutSelectElements(IntPtr he, [MarshalAs(UnmanagedType.LPStr)] string cssSelectors,
           HTMLayoutElementCallback callback, IntPtr param);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLayoutGetChildrenCount(IntPtr he, ref uint count);

        [DllImport(HTMLitePath)]
        private extern static uint HTMLayoutGetNthChild(IntPtr he, uint n, ref IntPtr hChild);

        private static void ThrowOnError(uint result)
        {
            if(result != 0)
            {
                throw new Exception(MessageFromCode(result));
            }
        }

        private static string MessageFromCode(uint errorCode)
        {
            switch((Result)errorCode)
            {
            case Result.Print_Success:
                return "No error.";
            case Result.Print_Failure_InvalidHandle:
                return "Invalid handle.";
            case Result.Print_Failure_InvalidFormat:
                return "Invalid format.";
            case Result.Print_Failure_FileNotFound:
                return "File not found.";
            case Result.Print_Failure_InvalidParameter:
                return "Invalid parameter.";
            case Result.Print_Failure_InvalidState:
                return "Invalid state.";
            default:
                return String.Format("Unknown error code {0}.", errorCode);
            }
        }
        #endregion
    }
}
