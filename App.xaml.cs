using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using nextCMIXGUI_WinUI.Views;

namespace nextCMIXGUI_WinUI
{
    public partial class App : Application
    {
        private Window m_window;
        public Window MainWindow => m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            m_window = new Window();
            Frame rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            
            // Set App title
            m_window.Title = "next-CMIXGUI";

            m_window.Content = rootFrame;
            
            // Navigate to our Views.MainPage
            rootFrame.Navigate(typeof(MainPage), e.Arguments);
            m_window.Activate();
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
