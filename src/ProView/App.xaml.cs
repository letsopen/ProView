using System;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ProView.Views;

namespace ProView
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // 挂起路径在部分 SDK/投影下对 SuspendingEventArgs.GetDeferral 映射异常；使用 Exiting 释放视图模型资源。
            CoreApplication.Exiting += (s, e) =>
            {
                if (Window.Current?.Content is Frame frame && frame.Content is MainPage mainPage)
                {
                    mainPage.DisposeViewModel();
                }
            };
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            EnsureRootFrame();
            if (e.PrelaunchActivated)
            {
                return;
            }

            Frame rootFrame = (Frame)Window.Current.Content;
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), null);
            }

            Window.Current.Activate();
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            EnsureRootFrame();
            Frame rootFrame = (Frame)Window.Current.Content;
            if (args.Files.Count > 0 && args.Files[0] is StorageFile file)
            {
                rootFrame.Navigate(typeof(MainPage), file);
            }
            else
            {
                rootFrame.Navigate(typeof(MainPage), null);
            }

            Window.Current.Activate();
        }

        private static void EnsureRootFrame()
        {
            if (Window.Current.Content is Frame)
            {
                return;
            }

            Frame rootFrame = new Frame();
            Window.Current.Content = rootFrame;
        }

    }
}
