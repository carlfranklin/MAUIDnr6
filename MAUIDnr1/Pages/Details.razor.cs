using MAUIDnr1.Shared;

namespace MAUIDnr1.Pages;
public partial class Details : ComponentBase
{
    [CascadingParameter]
    public CascadingAppState AppState { get; set; }

    [Parameter]
    public string ShowNumber { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (ShowNumber != null && ShowNumber != "playlist")
        {
            // load the details for the specified show number
            try
            {
                AppState.GetOnlineStatus();
                int showNumber = Convert.ToInt32(ShowNumber);
                await AppState.LoadShow(showNumber);
            }
            catch (Exception ex)
            {
            }
        }
        else if (ShowNumber == "playlist")
        {
            // load and play the selected playlist
            try
            {
                // do we have a selected playlist, and are there shows in it?
                if (AppState.SelectedPlayList == null) return;
                if (AppState.SelectedPlayList.Shows.Count == 0) return;
                // All systems are go!
                AppState.PlayingPlayList = true;
                AppState.GetOnlineStatus();
                // get the first show in the PlayList
                var showNumber = AppState.SelectedPlayList.Shows.First().ShowNumber;
                // get the show with the details
                await AppState.LoadShow(showNumber);

                if (AppState.ThisShow != null)
                {
                    // set the count (for the UI)
                    AppState.PlaylistShowCount = AppState.SelectedPlayList.Shows.Count;
                    // set the index (for the UI)
                    AppState.PlaylistShowIndex = 1;
                    // Play!
                    await AppState.PlayAudio();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}