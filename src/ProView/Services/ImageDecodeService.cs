using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using ProView.Interop;

namespace ProView.Services
{
    /// <summary>
    /// 使用 Windows.Graphics.Imaging（WIC）异步解码，不引入第三方图像库。
    /// </summary>
    public sealed class ImageDecodeService
    {
        public async Task<SoftwareBitmap> DecodeAsync(StorageFile file)
        {
            using (IRandomAccessStream stream = await WinRtAsync.AsTask(file.OpenAsync(FileAccessMode.Read)).ConfigureAwait(false))
            {
                BitmapDecoder decoder = await WinRtAsync.AsTask(BitmapDecoder.CreateAsync(stream)).ConfigureAwait(false);
                return await WinRtAsync.AsTask(decoder.GetSoftwareBitmapAsync()).ConfigureAwait(false);
            }
        }
    }
}
