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
using Windows.UI.Xaml.Media;
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

        // 缩放和平移状态
        private double _scale = 1.0;
        private double _offsetX = 0;
        private double _offsetY = 0;
        
        // 拖拽状态
        private bool _isDragging = false;
        private double _dragStartX;
        private double _dragStartY;
        private double _dragStartOffsetX;
        private double _dragStartOffsetY;

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
            try
            {
                await CheckFileSystemPermissionAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"权限检查失败: {ex.Message}");
            }
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private async Task CheckFileSystemPermissionAsync()
        {
            try
            {
                // 尝试访问一个常见路径来检测权限
                var testFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            }
            catch (UnauthorizedAccessException)
            {
                await ShowPermissionDialogAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"权限检查异常: {ex.Message}");
                // 其他异常不弹窗，静默处理
            }
        }

        private async Task ShowPermissionDialogAsync()
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "需要文件系统权限",
                    Content = "ProView 需要文件系统访问权限才能浏览图片。\n\n请点击\"打开设置\"，在隐私设置中开启\"文件系统\"权限，然后重新打开应用。",
                    PrimaryButtonText = "打开设置",
                    CloseButtonText = "稍后再说"
                };

                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示权限对话框失败: {ex.Message}");
            }
        }

        private void OnCanvasWheel(object sender, PointerRoutedEventArgs e)
        {
            if (_currentImage == null) return;

            var pointerPoint = e.GetCurrentPoint(ImageCanvas);
            double mouseX = pointerPoint.Position.X;
            double mouseY = pointerPoint.Position.Y;

            var delta = pointerPoint.Properties.MouseWheelDelta;

            // 计算鼠标在图片上的位置（图片坐标）
            double imageX = (mouseX - _offsetX) / _scale;
            double imageY = (mouseY - _offsetY) / _scale;

            // 计算新的缩放比例
            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            double newScale = _scale * zoomFactor;

            // 限制缩放范围
            newScale = Math.Max(0.1, Math.Min(10.0, newScale));

            // 计算新的偏移量，使鼠标位置保持对齐
            _offsetX = mouseX - imageX * newScale;
            _offsetY = mouseY - imageY * newScale;
            _scale = newScale;

            UpdateImageTransform();
            e.Handled = true;
        }

        private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_currentImage == null) return;

            var pointerPoint = e.GetCurrentPoint(ImageCanvas);
            _isDragging = true;
            _dragStartX = pointerPoint.Position.X;
            _dragStartY = pointerPoint.Position.Y;
            _dragStartOffsetX = _offsetX;
            _dragStartOffsetY = _offsetY;

            ImageCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;

            var pointerPoint = e.GetCurrentPoint(ImageCanvas);
            double deltaX = pointerPoint.Position.X - _dragStartX;
            double deltaY = pointerPoint.Position.Y - _dragStartY;

            _offsetX = _dragStartOffsetX + deltaX;
            _offsetY = _dragStartOffsetY + deltaY;

            UpdateImageTransform();
            e.Handled = true;
        }

        private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ImageCanvas.ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void UpdateImageTransform()
        {
            ImageTransform.ScaleX = _scale;
            ImageTransform.ScaleY = _scale;
            ImageTransform.TranslateX = _offsetX;
            ImageTransform.TranslateY = _offsetY;
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
                await LoadSiblingImagesAsync(file);

                ImageCanvas.Opacity = 0;

                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;

                OpenPrompt.Visibility = Visibility.Collapsed;
                ShowInfo();

                _currentIndex = _imageFiles.IndexOf(file);
                UpdateIndexDisplay();

                await Task.Delay(50);

                FitImageToView();

                ImageCanvas.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
                ImageCanvas.Opacity = 1;
            }
        }

        private void FitImageToView()
        {
            if (_currentImage == null) return;

            double imageWidth = _currentImage.ImageWidth;
            double imageHeight = _currentImage.ImageHeight;
            double viewWidth = ImageCanvas.ActualWidth;
            double viewHeight = ImageCanvas.ActualHeight;

            if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0) return;

            double scaleX = viewWidth / imageWidth;
            double scaleY = viewHeight / imageHeight;
            double scaleFactor = Math.Min(scaleX, scaleY);

            _scale = (scaleFactor < 1.0) ? scaleFactor : 1.0;
            _scale = Math.Max(0.1, Math.Min(10.0, _scale));

            double scaledWidth = imageWidth * _scale;
            double scaledHeight = imageHeight * _scale;
            _offsetX = (viewWidth - scaledWidth) / 2;
            _offsetY = (viewHeight - scaledHeight) / 2;

            UpdateImageTransform();
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
            
            ImageCanvas.Opacity = 0;
            
            try
            {
                _currentImage = new ImageFileInfo(file);
                await _currentImage.InitializeAsync();

                var bitmap = await _currentImage.GetImageSourceAsync();
                MainImage.Source = bitmap;

                UpdateIndexDisplay();
                ShowInfo();

                await Task.Delay(50);

                FitImageToView();

                ImageCanvas.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
                ImageCanvas.Opacity = 1;
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
