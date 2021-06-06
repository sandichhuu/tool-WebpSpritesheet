using System;
using UnityEngine;

namespace WebP.Unity
{
    using WebP.NativeWrapper.Dec;

    internal static class WebPHelper
    {
        public static unsafe void GetWebPDimensions(byte[] lData, out int lWidth, out int lHeight)
        {
            fixed (byte* lDataPtr = lData)
            {
                lWidth = 0;
                lHeight = 0;
                if (Decode.WebPGetInfo((IntPtr)lDataPtr, (UIntPtr)lData.Length, ref lWidth, ref lHeight) == 0)
                {
                    throw new Exception("Invalid WebP header detected");
                }
            }
        }

        public static unsafe byte[] LoadRGBAFromWebP(byte[] lData, ref int lWidth, ref int lHeight, bool lMipmaps, out Error lError)
        {
            lError = 0;
            byte[] lRawData = null;
            int lLength = lData.Length;

            fixed (byte* lDataPtr = lData)
            {
                // If mipmaps are requested we need to create 1/3 more memory for the mipmaps to be generated in.
                int numBytesRequired = lWidth * lHeight * 4;
                if (lMipmaps)
                {
                    numBytesRequired = Mathf.CeilToInt((numBytesRequired * 4.0f) / 3.0f);
                }

                lRawData = new byte[numBytesRequired];
                fixed (byte* lRawDataPtr = lRawData)
                {
                    int lStride = 4 * lWidth;

                    // As we have to reverse the y order of the data, we pass through a negative stride and
                    // pass through a pointer to the last line of the data.
                    byte* lTmpDataPtr = lRawDataPtr + (lHeight - 1) * lStride;

                    WebPDecoderConfig config = new WebPDecoderConfig();

                    if (Decode.WebPInitDecoderConfig(ref config) == 0)
                    {
                        throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
                    }

                    // Set up decode options
                    config.options.use_threads = 1;
                    config.options.scaled_width = lWidth;
                    config.options.scaled_height = lHeight;

                    // read the .webp input file information
                    VP8StatusCode result = Decode.WebPGetFeatures((IntPtr)lDataPtr, (UIntPtr)lLength, ref config.input);
                    if (result != VP8StatusCode.VP8_STATUS_OK)
                    {
                        throw new Exception(string.Format("Failed WebPGetFeatures with error {0}.", result.ToString()));
                    }

                    // specify the output format
                    config.output.colorspace = WEBP_CSP_MODE.MODE_RGBA;
                    config.output.u.RGBA.rgba = (IntPtr)lTmpDataPtr;
                    config.output.u.RGBA.stride = -lStride;
                    config.output.u.RGBA.size = (UIntPtr)(lHeight * lStride);
                    config.output.height = lHeight;
                    config.output.width = lWidth;
                    config.output.is_external_memory = 1;

                    // Decode
                    result = Decode.WebPDecode((IntPtr)lDataPtr, (UIntPtr)lLength, ref config);
                    if (result != VP8StatusCode.VP8_STATUS_OK)
                    {
                        throw new Exception(string.Format("Failed WebPDecode with error {0}.", result.ToString()));
                    }
                }
                lError = Error.Success;
            }
            return lRawData;
        }

        public static unsafe Texture2D CreateTexture2D(byte[] lData, bool lMipmaps, bool lLinear, out Error lError)
        {
            Texture2D lTexture2D = null;
            GetWebPDimensions(lData, out var lWidth, out var lHeight);
            var lRawData = LoadRGBAFromWebP(lData, ref lWidth, ref lHeight, lMipmaps, out lError);

            if (lError == Error.Success)
            {
                lTexture2D = new Texture2D(lWidth, lHeight, TextureFormat.RGBA32, lMipmaps, lLinear);
                lTexture2D.LoadRawTextureData(lRawData);
                lTexture2D.Apply(lMipmaps, true);
            }

            return lTexture2D;
        }
    }
}