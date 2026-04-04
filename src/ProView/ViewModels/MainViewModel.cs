using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ProView.Interop;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using ProView.Services;

namespace ProView.ViewModels
{
    /// <summary>
    /// 会话状态：目录文件列表、当前索引、当前位图源。
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ImageDecodeService _decodeService = new ImageDecodeService();
        private readonly FolderEnumerationService _folderService = new FolderEnumerationService();
        private bool _disposed;

        private IReadOnlyList<StorageFile> _files = Array.Empty<StorageFile>();
        private int _index;
        private SoftwareBitmap _bitmap;
        private SoftwareBitmapSource _bitmapSource;
        private string _errorMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public SoftwareBitmapSource BitmapSource
        {
            get => _bitmapSource;
            private set
            {
                if (_bitmapSource != value)
                {
                    _bitmapSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task InitializeAsync(StorageFile startFile)
        {
            if (startFile == null)
            {
                throw new ArgumentNullException(nameof(startFile));
            }

            _files = await _folderService.GetImageFilesInSameFolderAsync(startFile);
            _index = 0;
            for (int i = 0; i < _files.Count; i++)
            {
                if (string.Equals(_files[i].Path, startFile.Path, StringComparison.OrdinalIgnoreCase))
                {
                    _index = i;
                    break;
                }
            }

            await LoadCurrentAsync();
        }

        public void ShowOpenFileHint()
        {
            ErrorMessage = "请通过资源管理器「打开方式」或文件关联打开一张图片。";
            DisposeBitmapResources();
            BitmapSource = null;
        }

        public async Task GoNextAsync()
        {
            if (_files.Count == 0)
            {
                return;
            }

            _index = (_index + 1) % _files.Count;
            await LoadCurrentAsync();
        }

        public async Task GoPreviousAsync()
        {
            if (_files.Count == 0)
            {
                return;
            }

            _index = (_index - 1 + _files.Count) % _files.Count;
            await LoadCurrentAsync();
        }

        private async Task LoadCurrentAsync()
        {
            if (_files.Count == 0)
            {
                return;
            }

            StorageFile file = _files[_index];
            try
            {
                SoftwareBitmap decoded = await _decodeService.DecodeAsync(file);
                await SetBitmapAsync(decoded);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = string.IsNullOrEmpty(ex.Message) ? "无法解码此文件。" : ex.Message;
                await SetBitmapAsync(null);
            }
        }

        private async Task SetBitmapAsync(SoftwareBitmap newBitmap)
        {
            DisposeBitmapResources();
            _bitmap = newBitmap;
            if (_bitmap == null)
            {
                BitmapSource = null;
                return;
            }

            var source = new SoftwareBitmapSource();
            await WinRtAsync.AsTask(source.SetBitmapAsync(_bitmap));
            BitmapSource = source;
        }

        private void DisposeBitmapResources()
        {
            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }

            BitmapSource = null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DisposeBitmapResources();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
