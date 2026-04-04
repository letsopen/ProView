using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ProView.ViewModels;

namespace ProView.Views
{
    public sealed partial class MainPage : Page
    {
        private bool _viewModelDisposed;
        private const double MinScale = 0.1;
        private const double MaxScale = 10.0;
        private const double ZoomStep = 1.08;

        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.BitmapSource))
            {
                ResetImageTransform();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Focus(FocusState.Programmatic);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is StorageFile file)
            {
                try
                {
                    await ViewModel.InitializeAsync(file);
                }
                catch (Exception)
                {
                    ViewModel.ShowOpenFileHint();
                }
            }
            else
            {
                ViewModel.ShowOpenFileHint();
            }
        }

        private async void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Right)
            {
                await ViewModel.GoNextAsync();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Left)
            {
                await ViewModel.GoPreviousAsync();
                e.Handled = true;
            }
        }

        private void RootGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.BitmapSource == null)
            {
                return;
            }

            Windows.UI.Input.PointerPoint point = e.GetCurrentPoint(ImageView);
            Windows.Foundation.Point pos = point.Position;
            int delta = point.Properties.MouseWheelDelta;
            if (delta == 0)
            {
                return;
            }

            double zoomFactor = delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            double newScale = ImageComposite.ScaleX * zoomFactor;
            newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));
            double factor = newScale / ImageComposite.ScaleX;

            double x = pos.X;
            double y = pos.Y;
            ImageComposite.TranslateX = x - (x - ImageComposite.TranslateX) * factor;
            ImageComposite.TranslateY = y - (y - ImageComposite.TranslateY) * factor;
            ImageComposite.ScaleX = newScale;
            ImageComposite.ScaleY = newScale;

            e.Handled = true;
        }

        private void ResetImageTransform()
        {
            ImageComposite.ScaleX = 1;
            ImageComposite.ScaleY = 1;
            ImageComposite.TranslateX = 0;
            ImageComposite.TranslateY = 0;
        }

        /// <summary>
        /// 供 App.OnSuspending 调用，释放位图与会话资源。
        /// </summary>
        public void DisposeViewModel()
        {
            if (_viewModelDisposed)
            {
                return;
            }

            _viewModelDisposed = true;
            ViewModel.Dispose();
        }
    }
}
