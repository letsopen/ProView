using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using ProView.Interop;

namespace ProView.Services
{
    /// <summary>
    /// 枚举与当前文件同目录下的图片文件（内存列表，无索引库）。
    /// </summary>
    public sealed class FolderEnumerationService
    {
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff", ".jxr", ".wdp"
        };

        public async Task<IReadOnlyList<StorageFile>> GetImageFilesInSameFolderAsync(StorageFile currentFile)
        {
            StorageFolder folder = await WinRtAsync.AsTask(currentFile.GetParentAsync()).ConfigureAwait(false);
            if (folder == null)
            {
                return new[] { currentFile };
            }

            IReadOnlyList<StorageFile> files = await WinRtAsync.AsTask(folder.GetFilesAsync()).ConfigureAwait(false);
            List<StorageFile> images = files
                .Where(f => ImageExtensions.Contains(System.IO.Path.GetExtension(f.Name)))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (images.Count == 0)
            {
                return new[] { currentFile };
            }

            return images;
        }
    }
}
