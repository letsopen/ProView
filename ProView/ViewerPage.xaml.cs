//  ---------------------------------------------------------------------------------
//  ProView - Minimalist Image Viewer
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace ProView
{
    public sealed partial class ViewerPage : Page
    {
        private List<StorageFile> _imageFiles = new List<StorageFile>();
        private int _currentIndex = -1;
        private ImageFileInfo _currentImage;
        private bool _isInfoVisible = false;
        private DispatcherTimer _infoTimer;

        // 支持的图片格式
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        public ViewerPage()
        {
            this.InitializeComponent();
            SetupDragDrop();
            SetupInfoTimer();
            Window.Current.CoreWindow.PointerMoved += OnPointerMove;
        }

        private void SetupInfoTimer()
        {
            _infoTimer = new DispatcherTimer();
            _infoTimer.Interval = TimeSpan.FromSeconds(3);
            _infoTimer.Tick += (s, e) =>
            {
                _infoTimer.Stop();
                HideInfo();
            };
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.Drop += OnDrop;
            this.DragOver += OnDragOver;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var file = items.FirstOrDefault() as StorageFile;
                    if (file != null && IsImageFile(file))
                    {
                        await LoadImageAsync(file);
                    }
                }
            }
        }

        private void OnPointerMove(object sender, PointerEventArgs e)
        {
            if (_currentImage != null)
            {
                ShowInfo();
                _infoTimer.Stop();
                _infoTimer.Start();
            }
        }

        private void ShowInfo()
        {
            if (_currentImage != null && !_isInfoVisible)
            {
                _isInfoVisible = true;
                FileNameText.Visibility = Visibility.Visible;
                IndexText.Visibility = Visibility.Visible;
            }
        }

        private void HideInfo()
        {
            if (_isInfoVisible)
            {
                _isInfoVisible = false;
                FileNameText.Visibility = Visibility.Collapsed;
                IndexText.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.O:
                    await OpenFilePicker();
                    break;
                case VirtualKey.Left:
                    await NavigatePrevious();
                    break;
                case VirtualKey.Right:
                    await NavigateNext();
                    break;
                case VirtualKey.F:
                    ToggleFullScreen();
                    break;
                case VirtualKey.Escape:
                    if (IsFullScreen())
                    {
                        ExitFullScreen();
                    }
                    break;
            }
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // ScrollViewer 已内置缩放功能，这里可以添加以鼠标为中心的缩放增强
            // 当前使用 UWP 原生 ScrollViewer 缩放行为
        }

        private async Task OpenFilePicker()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            foreach (var ext in SupportedExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadImageAsync(file);
            }
        }

        public async Task OpenFileAsync(StorageFile file)
        {
            await LoadImageAsync(file);
        }

        private bool IsImageFile(StorageFile file)
        {
            var ext = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            return SupportedExtensions.Contains(ext);
        }

        private async Task LoadImageAsync(StorageFile file)
        {
            try
            {
                // 加载同级目录图片列表
                await LoadSiblingImagesAsync(file);

                // 加载当前图片
                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;

                // 重置缩放
                ImageScroller.ZoomToFactor(1.0f);

                // 更新 UI
                OpenPrompt.Visibility = Visibility.Collapsed;
                ShowInfo();

                // 更新索引显示
                _currentIndex = _imageFiles.IndexOf(file);
                UpdateIndexDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
            }
        }

        private async Task LoadSiblingImagesAsync(StorageFile file)
        {
            _imageFiles.Clear();
            _currentIndex = -1;

            try
            {
                var folder = file.GetParentAsync() != null ? await file.GetParentAsync() : null;
                if (folder != null)
                {
                    var allFiles = await folder.GetFilesAsync();
                    foreach (var f in allFiles.OrderBy(f => f.Name))
                    {
                        if (IsImageFile(f))
                        {
                            _imageFiles.Add(f);
                        }
                    }
                    _currentIndex = _imageFiles.IndexOf(file);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载目录失败: {ex.Message}");
            }
        }

        private void UpdateIndexDisplay()
        {
            if (_currentIndex >= 0 && _imageFiles.Count > 0)
            {
                IndexText.Text = $"{_currentIndex + 1} / {_imageFiles.Count}";
                if (_currentImage != null)
                {
                    FileNameText.Text = _currentImage.ImageName;
                }
            }
        }

        private async Task NavigatePrevious()
        {
            if (_imageFiles.Count == 0) return;

            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _imageFiles.Count - 1; // 循环到末尾
            }

            await LoadImageAt(_currentIndex);
        }

        private async Task NavigateNext()
        {
            if (_imageFiles.Count == 0) return;

            _currentIndex++;
            if (_currentIndex >= _imageFiles.Count)
            {
                _currentIndex = 0; // 循环到开头
            }

            await LoadImageAt(_currentIndex);
        }

        private async Task LoadImageAt(int index)
        {
            if (index < 0 || index >= _imageFiles.Count) return;

            var file = _imageFiles[index];
            try
            {
                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;

                // 重置缩放
                ImageScroller.ZoomToFactor(1.0f);

                UpdateIndexDisplay();
                ShowInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
            }
        }

        private void ToggleFullScreen()
        {
            var view = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                view.TryEnterFullScreenMode();
            }
        }

        private bool IsFullScreen()
        {
            return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        private void ExitFullScreen()
        {
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }
    }
}
