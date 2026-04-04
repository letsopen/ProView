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
        private bool _hasFileSystemPermission = false;

        // 支持的图片格式
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        public ViewerPage()
        {
            this.InitializeComponent();
            SetupInfoTimer();
            Window.Current.CoreWindow.PointerMoved += OnPointerMove;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // 检测文件系统权限
            await CheckFileSystemPermissionAsync();
            
            // 注册 CoreWindow 级别的键盘事件
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            
            // 使用 AddHandler 注册滚轮事件，确保可以接收已处理的事件
            ImageScroller.AddHandler(ScrollViewer.PointerWheelChangedEvent, new PointerEventHandler(OnScrollViewerWheel), true);
        }

        private async Task CheckFileSystemPermissionAsync()
        {
            try
            {
                // 尝试访问一个常见路径来检测权限
                var testFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                _hasFileSystemPermission = true;
            }
            catch (UnauthorizedAccessException)
            {
                _hasFileSystemPermission = false;
                await ShowPermissionDialogAsync();
            }
            catch (Exception)
            {
                // 其他异常也认为没有权限
                _hasFileSystemPermission = false;
            }
        }

        private async Task ShowPermissionDialogAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "需要文件系统权限",
                Content = "ProView 需要文件系统访问权限才能浏览图片。\n\n请点击\"打开设置\"，在隐私设置中开启\"文件系统\"权限。",
                PrimaryButtonText = "打开设置",
                CloseButtonText = "稍后再说"
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // 打开系统设置页面
                await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
            }
        }

        private void OnScrollViewerWheel(object sender, PointerRoutedEventArgs e)
        {
            // 完全拦截滚轮事件用于缩放
            e.Handled = true;
            
            // 获取滚轮方向
            var delta = e.GetCurrentPoint(ImageScroller).Properties.MouseWheelDelta;
            
            // 获取当前缩放因子
            float currentZoom = ImageScroller.ZoomFactor;
            
            // 计算新的缩放因子（每次滚动调整10%）
            float zoomDelta = delta > 0 ? 0.1f : -0.1f;
            float newZoom = currentZoom + currentZoom * zoomDelta;
            
            // 限制在有效范围内
            newZoom = Math.Max(0.1f, Math.Min(10.0f, newZoom));
            
            // 应用缩放（禁用动画）
            ImageScroller.ChangeView(null, null, newZoom, true);
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.O:
                    _ = OpenFilePicker();
                    args.Handled = true;
                    break;
                case VirtualKey.Left:
                    _ = NavigatePrevious();
                    args.Handled = true;
                    break;
                case VirtualKey.Right:
                    _ = NavigateNext();
                    args.Handled = true;
                    break;
                case VirtualKey.F:
                    ToggleFullScreen();
                    args.Handled = true;
                    break;
                case VirtualKey.Escape:
                    if (IsFullScreen())
                    {
                        ExitFullScreen();
                        args.Handled = true;
                    }
                    break;
            }
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

        private async void OnOpenPromptTapped(object sender, TappedRoutedEventArgs e)
        {
            await OpenFilePicker();
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

                // 先隐藏 ScrollViewer
                ImageScroller.Opacity = 0;

                // 加载当前图片
                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;
                MainImage.Opacity = 1;

                // 设置容器尺寸为图片原始尺寸
                ImageContainer.Width = _currentImage.ImageWidth;
                ImageContainer.Height = _currentImage.ImageHeight;

                // 更新 UI
                OpenPrompt.Visibility = Visibility.Collapsed;
                ShowInfo();

                // 更新索引显示
                _currentIndex = _imageFiles.IndexOf(file);
                UpdateIndexDisplay();

                // 等待布局更新
                await Task.Delay(100);

                // 后台应用缩放和居中
                FitImageToView();

                // 显示
                ImageScroller.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
                ImageScroller.Opacity = 1;
            }
        }

        private void FitImageToView()
        {
            if (_currentImage == null) return;

            double imageWidth = _currentImage.ImageWidth;
            double imageHeight = _currentImage.ImageHeight;
            double viewWidth = ImageScroller.ActualWidth;
            double viewHeight = ImageScroller.ActualHeight;

            if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0) return;

            // 计算需要的缩放比例（只缩小不放大）
            double scaleX = viewWidth / imageWidth;
            double scaleY = viewHeight / imageHeight;
            double scaleFactor = Math.Min(scaleX, scaleY);

            // 只缩小不放大：如果图片比可视区域小，保持原始大小
            float zoomFactor = (scaleFactor < 1.0) ? (float)scaleFactor : 1.0f;

            // 确保缩放因子在有效范围内
            zoomFactor = Math.Max(0.1f, Math.Min(10.0f, zoomFactor));

            // 计算缩放后的尺寸
            double scaledWidth = imageWidth * zoomFactor;
            double scaledHeight = imageHeight * zoomFactor;

            // 计算居中位置
            double scrollX = (viewWidth - scaledWidth) / 2;
            double scrollY = (viewHeight - scaledHeight) / 2;

            // 确保滚动位置合理
            scrollX = Math.Max(0, scrollX);
            scrollY = Math.Max(0, scrollY);

            // 禁用动画，直接应用
            ImageScroller.ChangeView(scrollX, scrollY, zoomFactor, true);
        }

        private async Task LoadSiblingImagesAsync(StorageFile file)
        {
            _imageFiles.Clear();
            _currentIndex = -1;

            try
            {
                StorageFolder folder = await file.GetParentAsync();
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
            
            // 先隐藏 ScrollViewer
            ImageScroller.Opacity = 0;
            
            try
            {
                // 加载新图片
                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;

                // 设置容器尺寸
                ImageContainer.Width = _currentImage.ImageWidth;
                ImageContainer.Height = _currentImage.ImageHeight;

                UpdateIndexDisplay();
                ShowInfo();

                // 等待布局更新
                await Task.Delay(100);

                // 后台应用缩放和居中（禁用动画）
                FitImageToView();

                // 显示
                ImageScroller.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
                ImageScroller.Opacity = 1;
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
