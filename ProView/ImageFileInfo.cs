//  ---------------------------------------------------------------------------------
//  ProView - Minimalist Image Viewer
//  ---------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ProView
{
    /// <summary>
    /// Minimal image file info - no editing properties, just essential display info
    /// </summary>
    public class ImageFileInfo : INotifyPropertyChanged
    {
        public ImageFileInfo(StorageFile imageFile)
        {
            ImageFile = imageFile;
            ImageName = imageFile.Name;
        }

        public StorageFile ImageFile { get; }
        public string ImageName { get; }
        public uint ImageWidth => _imageProperties?.Width ?? 0;
        public uint ImageHeight => _imageProperties?.Height ?? 0;
        public string ImageDimensions => $"{ImageWidth} x {ImageHeight}";

        private ImageProperties _imageProperties;
        private BitmapImage _thumbnail;
        private BitmapImage _fullImage;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public async Task InitializeAsync()
        {
            _imageProperties = await ImageFile.Properties.GetImagePropertiesAsync();
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
            OnPropertyChanged(nameof(ImageDimensions));
        }

        public async Task<BitmapImage> GetThumbnailAsync()
        {
            if (_thumbnail != null) return _thumbnail;

            try
            {
                var thumbnail = await ImageFile.GetThumbnailAsync(ThumbnailMode.PicturesView);
                _thumbnail = new BitmapImage();
                _thumbnail.SetSource(thumbnail);
                thumbnail.Dispose();
            }
            catch
            {
                _thumbnail = new BitmapImage();
            }
            return _thumbnail;
        }

        public async Task<BitmapImage> GetImageSourceAsync()
        {
            if (_fullImage != null) return _fullImage;

            try
            {
                using (IRandomAccessStream fileStream = await ImageFile.OpenReadAsync())
                {
                    _fullImage = new BitmapImage();
                    await _fullImage.SetSourceAsync(fileStream);
                }
            }
            catch
            {
                _fullImage = new BitmapImage();
            }
            return _fullImage;
        }
    }
}
