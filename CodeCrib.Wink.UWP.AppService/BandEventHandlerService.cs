using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Microsoft.Band;
using Windows.Foundation.Collections;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;

namespace CodeCrib.Wink.UWP.Band
{
    public sealed class BandEventHandlerService : IBackgroundTask
    {
        private BackgroundTaskDeferral backgroundTaskDeferral;
        private AppServiceConnection appServiceconnection;

        private IBandClient bandClient;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral so that the service isn't terminated until we complete the deferral
            this.backgroundTaskDeferral = taskInstance.GetDeferral();

            // Associate a cancellation handler with the background task.
            taskInstance.Canceled += OnTaskCanceled;

            // Add our handlers for tile events
            BackgroundTileEventHandler.Instance.TileOpened += EventHandler_TileOpened;
            BackgroundTileEventHandler.Instance.TileClosed += EventHandler_TileClosed;
            BackgroundTileEventHandler.Instance.TileButtonPressed += EventHandler_TileButtonPressed;

            // Retrieve the app service connection and set up a listener for incoming app service requests.
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            appServiceconnection = details.AppServiceConnection;
            appServiceconnection.RequestReceived += OnRequestReceived;
        }

        /// <summary>
        /// OnTaskCanceled() is called when the task is canceled. 
        /// The task is cancelled when the client app disposes the AppServiceConnection, 
        /// the client app is suspended, the OS is shut down or sleeps, 
        /// or the OS runs out of resources to run the task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            DisconnectBand();

            if (this.backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                this.backgroundTaskDeferral.Complete();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            ValueSet responseMessage = new ValueSet();

            // Decode the received message and call the appropriate event handler
            BackgroundTileEventHandler.Instance.HandleTileEvent(args.Request.Message);

            // Send the response message
            await args.Request.SendResponseAsync(responseMessage);

            // Complete the deferral so that the platform knows that we're done responding
            messageDeferral.Complete();
        }

        /// <summary>
        /// Handle the event that occurs when the user opens our tile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Data describing the event details</param>
        private void EventHandler_TileOpened(object sender, BandTileEventArgs<IBandTileOpenedEvent> e)
        {
            // e.TileEvent.TileId is the tile’s Guid.    
            // e.TileEvent.Timestamp is the DateTimeOffset of the event.     
            //LogEvent(String.Format("EventHandler_TileOpened: TileId={0} Timestamp={1}", e.TileEvent.TileId, e.TileEvent.Timestamp));

            // We create a Band connection when the tile is opened and keep it connected until the tile closes.
            ConnectBand();

            UpdatePageData();
        }

        /// <summary>
        /// Handle the event that occurs when our tile is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Data describing the event details</param>
        private void EventHandler_TileClosed(object sender, BandTileEventArgs<IBandTileClosedEvent> e)
        {
            // e.TileEvent.TileId is the tile’s Guid.    
            // e.TileEvent.Timestamp is the DateTimeOffset of the event.       
            //LogEvent(String.Format("EventHandler_TileClosed: TileId={0} Timestamp={1}", e.TileEvent.TileId, e.TileEvent.Timestamp));

            UpdatePageData();

            // Disconnect the Band now that the user has closed the tile.
            DisconnectBand();
        }

        /// <summary>
        /// Handle the event that occurs when the user presses a button on page of the tile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Data describing the event details</param>
        private void EventHandler_TileButtonPressed(object sender, BandTileEventArgs<IBandTileButtonPressedEvent> e)
        {
            // e.TileEvent.TileId is the tile’s Guid.    
            // e.TileEvent.Timestamp is the DateTimeOffset of the event.    
            // e.TileEvent.PageId is the Guid of our page with the button.    
            // e.TileEvent.ElementId is the value assigned to the button    
            //                       in our layout (i.e.,    
            //                       TilePageElementId.Button_PushMe). 
            //LogEvent(String.Format("EventHandler_TileButtonPressed: TileId={0} PageId={1} ElementId={2}", e.TileEvent.TileId, e.TileEvent.PageId, e.TileEvent.ElementId));

            // We should have a Band connection from the tile open event, but in case the OS unloaded our background code
            // between that event and this button press event, we restore the connection here as needed.
            ConnectBand();

            UpdatePageData();
        }

        private void ConnectBand()
        {
            if (this.bandClient == null)
            {
                try
                {
                    // Note that we specify isBackground = true here to avoid conflicting with any foreground app connection to the Band
                    Task<IBandInfo[]> getBands = BandClientManager.Instance.GetBandsAsync(isBackground: true);
                    getBands.Wait();
                    IBandInfo[] pairedBands = getBands.Result;

                    if (pairedBands.Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("CODECRIB - No Paired Band Found!");
                        //LogEvent("ERROR - No paired Band");
                    }


                    Task<IBandClient> connect = BandClientManager.Instance.ConnectAsync(pairedBands[0]);
                    connect.Wait();
                    this.bandClient = connect.Result;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CODECRIB - " + ex.ToString());
                    //LogEvent("ERROR - Unable to connect to Band");
                }
            }
        }
        private void DisconnectBand()
        {
            if (bandClient != null)
            {
                bandClient.Dispose();
                bandClient = null;
            }
        }

        /// <summary>
        /// Update the page of data displayed within our tile
        /// </summary>
        private void UpdatePageData()
        {
            WinkTile.UpdatePageData(this.bandClient);
        }
    }
}
