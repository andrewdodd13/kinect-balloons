using System;
using System.Runtime.InteropServices;

namespace BubblesClient.Utility
{
    /// <summary>
    /// Wraps the HTMLite library that renders HTML pages to images.
    /// </summary>
    public class HTMLite : IDisposable
    {
        private IntPtr hLite;

        public HTMLite()
        {
            this.hLite = HTMLiteCreateInstance();
            if(this.hLite == IntPtr.Zero)
            {
                throw new Exception("Could not load HTMLlite");
            }
        }

        ~HTMLite()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(hLite != IntPtr.Zero)
            {
                HTMLiteDestroyInstance(hLite);
                hLite = IntPtr.Zero;
            }
        }

        public void LoadHtmlFromMemory(string baseURI, byte[] data)
        {
            uint result = HTMLiteLoadHtmlFromMemory(hLite, baseURI, data, (uint)data.Length);
            ThrowOnError(result);
        }

        public void LoadHtmlFromFile(string path)
        {
            uint result = HTMLiteLoadHtmlFromFile(hLite, path);
            ThrowOnError(result);
        }

        public void Measure(int width, int height)
        {
            uint result = HTMLiteMeasure(hLite, width, height);
            ThrowOnError(result);
        }

        public void Render(IntPtr hDc, int x, int y, int sx, int sy)
        {
            uint result = HTMLiteRender(hLite, hDc, x, y, sx, sy);
            ThrowOnError(result);
        }

        public void RenderOnBitmap(IntPtr hBmp, int x, int y, int sx, int sy)
        {
            uint result = HTMLiteRenderOnBitmap(hLite, hBmp, x, y, sx, sy);
            ThrowOnError(result);
        }

        public void SetDataReady(string url, IntPtr data, uint dataSize)
        {
            uint result = HTMLiteSetDataReady(hLite, url, data, dataSize);
            ThrowOnError(result);
        }

        public void SetCallback(Callback cb)
        {
            uint result = HTMLiteSetCallback(hLite, (handle, msg) => cb(this, msg));
            ThrowOnError(result);
        }

        public T ReadMessage<T>(IntPtr pMsg) where T : struct
        {
            if(pMsg == IntPtr.Zero)
            {
                throw new ArgumentNullException("pMsg");
            }
            return (T)Marshal.PtrToStructure(pMsg, typeof(T));
        }

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

        public struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public uint code;
        };

        public struct NMHL_LOAD_DATA
        {
            public NMHDR hdr;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string uri;
            public IntPtr outData;
            public uint outDataSize;
            public uint dataType;
        }

        public delegate uint Callback(HTMLite h, IntPtr pMsg);
        #endregion

        #region P/invoke stuff
        private delegate uint HTMLCallback(IntPtr hLite, IntPtr pMsg);

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

        private void ThrowOnError(uint result)
        {
            if(result != 0)
            {
                throw new Exception(MessageFromCode(result));
            }
        }

        private string MessageFromCode(uint errorCode)
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
