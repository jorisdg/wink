using Microsoft.Band;
using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CodeCrib.Wink.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        App CurrentApp { get { return App.Current as App; } }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void PromptForCredentials()
        {
            Refresh.IsEnabled = false;
            Logout.IsEnabled = false;
            WinkPivot.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;

            StatusText.Text = "Credentials needed.";
        }

        private void ShowWink()
        {
            Refresh.IsEnabled = true;
            Logout.IsEnabled = true;
            WinkPivot.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Collapsed;

            this.RefreshWink();
        }

        private async void RefreshWink()
        {
            if (CurrentApp.Wink != null && CurrentApp.Wink.oAuth != null)
            {
                StatusText.Text = "Refreshing Wink Status...";
                try
                {
                    GroupList.ItemsSource = await CurrentApp.Wink.GetAllGroups();
                    DeviceList.ItemsSource = await CurrentApp.Wink.GetAllDevices();

                    StatusText.Text = "Successfully refreshed status";
                }
                catch (Lib.LoginNeededException)
                {
                    if (CurrentApp.Wink.oAuth == null)
                    {
                        this.PromptForCredentials();
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = ex.Message;
                }
            }
            else
            {
                this.PromptForCredentials();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.Wink == null || CurrentApp.Wink.oAuth == null)
            {
                this.PromptForCredentials();
            }
            else
            {
                this.ShowWink();
            }
        }

        private async void GroupAllPowered_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.Wink != null && CurrentApp.Wink.oAuth != null)
            {
                CheckBox checkBox = sender as CheckBox;

                if (checkBox != null)
                {
                    CodeCrib.Wink.Lib.Group group = checkBox.DataContext as CodeCrib.Wink.Lib.Group;
                    if (group != null)
                    {
                        StatusText.Text = string.Format("Setting Group Power to {0}...", checkBox.IsChecked == true);
                        try
                        {
                            await CurrentApp.Wink.SetGroup(group, checkBox.IsChecked == true);
                            StatusText.Text = string.Format("Successfully set Group Power to {0}", checkBox.IsChecked == true);
                        }
                        catch (Lib.LoginNeededException)
                        {
                            if (CurrentApp.Wink.oAuth == null)
                            {
                                this.PromptForCredentials();
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusText.Text = ex.Message;
                        }
                    }
                }
            }
            else
            {
                this.PromptForCredentials();
            }
        }

        private async void DevicePowered_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.Wink != null && CurrentApp.Wink.oAuth != null)
            {
                CheckBox checkBox = sender as CheckBox;

                if (checkBox != null)
                {
                    CodeCrib.Wink.Lib.Device device = checkBox.DataContext as CodeCrib.Wink.Lib.Device;
                    if (device != null)
                    {
                        StatusText.Text = string.Format("Setting Device Power to {0}...", checkBox.IsChecked == true);
                        try
                        {
                            await CurrentApp.Wink.SetDevice(device, checkBox.IsChecked == true);
                            StatusText.Text = string.Format("Successfully set Device Power to {0}", checkBox.IsChecked == true);
                        }
                        catch (Lib.LoginNeededException)
                        {
                            if (CurrentApp.Wink.oAuth == null)
                            {
                                this.PromptForCredentials();
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusText.Text = ex.Message;
                        }
                    }
                }
            }
            else
            {
                this.PromptForCredentials();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshWink();
        }

        private async void BandTile_Click(object sender, RoutedEventArgs e)
        {
            //CodeCrib.Wink.UWP.Band.WinkTile winkTile = new Band.WinkTile(StatusText);
            StatusText.Text = "Installing Band Tile";

            if (await CodeCrib.Wink.UWP.Band.WinkTile.InstallTile())
            {
                StatusText.Text = "Successfully installed Band Tile";
            }
            else
            {
                StatusText.Text = "Failed to install Band Tile";
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(UserName.Text) && !string.IsNullOrEmpty(Password.Text))
            {
                if (await CurrentApp.ConnectWink(UserName.Text, Password.Text))
                {
                    this.ShowWink();
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            CurrentApp.DisconnectWink();
            this.PromptForCredentials();
        }
    }
}
