using Microsoft.Band;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CodeCrib.Wink.UWP.Band
{
    public sealed class WinkTile
    {
        private static readonly Guid tileGuid = new Guid("724FA581-EB5D-493D-8506-2BA2B5913FAD");
        private static readonly Guid page1Guid = new Guid("00000000-0000-0000-0000-000000000001");
        private static short button1ElementId { get { return 1; } }
        private static short textElementId { get { return 2; } }

        Windows.UI.Xaml.Controls.TextBlock statusText;

        public WinkTile(Windows.UI.Xaml.Controls.TextBlock status)
        {
            statusText = status;
        }

        public static IAsyncOperation<bool> InstallTile()
        {
            return WinkTile.InstallTile(false).AsAsyncOperation();
        }

        public static IAsyncOperation<bool> InstallTileBackground()
        {
            return WinkTile.InstallTile(true).AsAsyncOperation();
        }

        private static async Task<bool> InstallTile(bool isBackground)
        {
            bool installed = false;

            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync(isBackground);
                if (pairedBands.Length >= 1)
                {
                    // Connect to Microsoft Band.
                    using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                    {
                        // Create a Tile with a TextButton and WrappedTextBlock on it.
                        BandTile winkTile = new BandTile(WinkTile.tileGuid)
                        {
                            Name = "Wink",
                            TileIcon = await LoadIcon("ms-appx:///Assets/BandTileLarge.png"),
                            SmallIcon = await LoadIcon("ms-appx:///Assets/BandTileSmall.png")
                        };

                        //TextButton button = new TextButton() { ElementId = WinkTile.button1ElementId, Rect = new PageRect(10, 5, 200, 30) };
                        //WrappedTextBlock textblock = new WrappedTextBlock() { ElementId = WinkTile.textElementId, Rect = new PageRect(10, 40, 200, 88) };
                        //PageElement[] elements = new PageElement[] { button, textblock };
                        //FilledPanel panel = new FilledPanel(elements) { Rect = new PageRect(0, 0, 220, 128) };
                        //winkTile.PageLayouts.Add(new PageLayout(panel));

                        GroupTile groupTile = new GroupTile();
                        winkTile.PageLayouts.Add(groupTile.Layout);
                        groupTile.LoadIconsAsync(winkTile);

                        // TODO: remove test code that checks text update feature actually works
                        groupTile.Data.ById<TextBlockData>(2).Text = "Testink";

                        // Remove the Tile from the Band, if present. An application won't need to do this everytime it runs. 
                        // But in case you modify this sample code and run it again, let's make sure to start fresh.
                        await bandClient.TileManager.RemoveTileAsync(WinkTile.tileGuid);

                        // Create the Tile on the Band.
                        await bandClient.TileManager.AddTileAsync(winkTile);

                        //PageData pageData = new PageData
                        //(
                        //    WinkTile.page1Guid,
                        //    0,
                        //    new TextButtonData(WinkTile.button1ElementId, "Lights Status"),
                        //    new WrappedTextBlockData(WinkTile.textElementId, "...")
                        //);
                        //await bandClient.TileManager.SetPagesAsync(WinkTile.tileGuid, pageData);

                        await bandClient.TileManager.SetPagesAsync(WinkTile.tileGuid, new PageData(WinkTile.page1Guid, 0,  groupTile.Data.All));

                        // Subscribe to background tile events
                        await bandClient.SubscribeToBackgroundTileEventsAsync(WinkTile.tileGuid);

                        installed = true;
                    }
                }
            }
            catch
            {
                installed = false;
            }

            return installed;
        }

        internal static void UpdatePageData(IBandClient bandClient)
        {
            if (bandClient != null)
            {
                try
                {
                    GroupTile groupTile = new GroupTile();

                    groupTile.Data.ById<TextBlockData>(2).Text = "Background Service";
                    var data = groupTile.Data.All;

                    bandClient.TileManager.SetPagesAsync(WinkTile.tileGuid, new PageData(WinkTile.page1Guid, 0, data)).Wait();
                }
                catch(Exception ex)
                {
                    //Exception a = ex;
                    System.Diagnostics.Debug.WriteLine("CODECRIB - " + ex.ToString());
                    //LogEvent("ERROR - Unable to update page data");
                }
            }
        }

        private static async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                Windows.UI.Xaml.Media.Imaging.WriteableBitmap bitmap = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }
    }
}
