#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace MAUIDnr1.Shared;

public partial class CascadingAppState : ComponentBase
{

    public static readonly int WindowWidth = 900;
    public static readonly int WindowHeight = 600;
#if WINDOWS
    public static Microsoft.UI.Windowing.AppWindow AppWindow { get; set; }
#endif

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Inject]
    private IAudioManager _audioManager { get; set; }

    [Inject]
    private NavigationManager _navigationManager { get; set; }

    [Inject]
    private ApiService _apiService { get; set; }

    // media player
    public IAudioPlayer? Player = null;

    // stream used for playing
    public FileStream? Stream = null;

    // Used to report current position
    public System.Timers.Timer? Timer = null;

    // state of the player
    public MediaState MediaState;

    public Show ThisShow { get; set; } = null;

    public int ShowNumber { get; set; }

    // Downloading, Playing, Paused or error
    private string audioMessage = string.Empty;
    public string AudioMessage
    {
        get => audioMessage;
        set
        {
            audioMessage = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // set true when playing through a playlist
    private bool playingPlayList = false;
    public bool PlayingPlayList
    {
        get => playingPlayList;
        set
        {
            playingPlayList = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // 1-based index of currently playing show
    private int playlistShowIndex = 0;
    public int PlaylistShowIndex
    {
        get => playlistShowIndex;
        set
        {
            playlistShowIndex = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // total number of shows in the playlist
    private int playlistShowCount = 0;
    public int PlaylistShowCount
    {
        get => playlistShowCount;
        set
        {
            playlistShowCount = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // percentage of audio played used to set progress bar value
    private double percentage = 0;
    public double Percentage
    {
        get => percentage;
        set
        {
            percentage = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // calculated from current position
    private string playPosition = "";
    public string PlayPosition
    {
        get => playPosition;
        set
        {
            playPosition = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // .5 for 'disabled' 
    private string controlsOpacity = ".5";
    public string ControlsOpacity
    {
        get => controlsOpacity;
        set
        {
            controlsOpacity = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // 1 for 'enabled'
    private string playOpacity = "1";
    public string PlayOpacity
    {
        get => playOpacity;
        set
        {
            playOpacity = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // image reacts when pressed
    private string playButtonClass = "imageButton";
    public string PlayButtonClass
    {
        get => playButtonClass;
        set
        {
            playButtonClass = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // see SetState to see in action
    private string pauseButtonClass = "";
    public string PauseButtonClass
    {
        get => pauseButtonClass;
        set
        {
            pauseButtonClass = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // see SetState to see in action
    private string stopButtonClass = "";
    public string StopButtonClass
    {
        get => stopButtonClass;
        set
        {
            stopButtonClass = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // see SetState to see in action
    private string rewindButtonClass = "";
    public string RewindButtonClass
    {
        get => rewindButtonClass;
        set
        {
            rewindButtonClass = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // see SetState to see in action
    private string forwardButtonClass = "";
    public string ForwardButtonClass
    {
        get => forwardButtonClass;
        set
        {
            forwardButtonClass = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // list of all playlists
    public ObservableCollection<PlayList> PlayLists { get; set; }
            = new ObservableCollection<PlayList>();

    // currently selected playlist
    public PlayList SelectedPlayList { get; set; }

    // path to the json file
    private static string playListJsonFile = "";

    // when true, and a playlist is selected, determines
    // whether or not to show all the playlist shows on main page.
    // otherwise, the current set of shows (filtered or not)
    // is shown.
    private bool showPlayListOnly = false;
    public bool ShowPlayListOnly
    {
        get => showPlayListOnly;
        set
        {
            showPlayListOnly = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // text shown in the "Show Playlist" toggle button.
    // see ToggleShowPlaylistOnly()
    public string ShowPlayListOnlyText = "Show Playlist";

    // the set of shows shown on the main page
    public ObservableCollection<Show> AllShows { get; set; }
        = new ObservableCollection<Show>();

    // the set of all show numbers used to get the next set
    // of shows when "Load More Shows" button is clicked
    public List<int> ShowNumbers { get; set; } = new List<int>();

    // the last show number shown on the main page
    private int lastShowNumber { get; set; }
    public int LastShowNumber
    {
        get => lastShowNumber;
        set
        {
            lastShowNumber = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // EpisodeFilter property on the main page
    private string episodeFilter { get; set; } = "";
    public string EpisodeFilter
    {
        get => episodeFilter;
        set
        {
            episodeFilter = value;
            InvokeAsync(StateHasChanged);
        }
    }

    // private fields used to save the state before replacing
    // the current view with the selected playlist shows.
    // see ToggleShowPlaylistOnly()
    private static string AllShowsBackupString { get; set; } = "";
    private static int LastShowNumberBackup;
    private static string BackupEpisodeFilter { get; set; } = "";


    // tells the UI (and therefore the ApiService) whether or
    // not we are online. 
    // Call GetOnlineStatus(); to set it
    public bool IsOnline;

    public async Task LoadShow(int ShowNumber)
    {
        await Cleanup();
        this.ShowNumber = ShowNumber;
        ThisShow = await _apiService.GetShowWithDetails(ShowNumber);
    }

    /// <summary>
    /// Skip forward 10 seconds
    /// </summary>
    public void Forward()
    {
        if (MediaState == MediaState.Playing)
        {
            var pos = Player.CurrentPosition + 10;
            if (pos < Player.Duration)
                Player.Seek(pos);
            else
                StopAudio();
        }
    }

    /// <summary>
    /// Stop
    /// </summary>
    public void StopAudio()
    {
        if (MediaState == MediaState.Playing)
        {
            PlayingPlayList = false;
            Player.Stop();
            SetState(MediaState.Stopped);
        }
    }

    /// <summary>
    /// Pause
    /// </summary>
    public void PauseAudio()
    {
        if (MediaState == MediaState.Playing)
        {
            Player.Pause();
            SetState(MediaState.Paused);
        }
    }

    /// <summary>
    /// Rewind 10 seconds (or to the beginning)
    /// </summary>
    public void Rewind()
    {
        if (MediaState == MediaState.Playing)
        {
            var pos = Player.CurrentPosition - 10;
            if (pos < 0)
                pos = 0;
            Player.Seek(pos);
        }
    }


    public async Task PlayAudio()
    {
        // are we paused?
        if (MediaState == MediaState.Paused && Player != null)
        {
            // yes. Continue playing
            Player.Play();
            SetState(MediaState.Playing);
            return;
        }

        // exit if there is no url specified
        if (string.IsNullOrEmpty(ThisShow.Mp3Url))
        {
            AudioMessage = "Please enter a URL to an MP3 file";
            return;
        }

        // Make sure we are stopped
        await Cleanup();

        // here we go!
        try
        {
            // This is where we are storing local audio files
            string cacheDir = FileSystem.Current.CacheDirectory;

            // get the fully qualified path to the local file
            var fileName = ThisShow.Mp3Url.Substring(8).Replace("/", "-");
            var localFile = $"{cacheDir}\\{fileName}";

            // download if need be
            if (!System.IO.File.Exists(localFile))
            {
                // let the user know we're trying to download
                AudioMessage = "Downloading...";

                // this code downloads the file from the URL
                using (var client = new HttpClient())
                {
                    var uri = new Uri(ThisShow.Mp3Url);
                    var response = await client.GetAsync(ThisShow.Mp3Url);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var fileInfo = new FileInfo(localFile);
                        using (var fileStream = fileInfo.OpenWrite())
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }

            // File exists now. Read it
            Stream = System.IO.File.OpenRead(localFile);

            Player = _audioManager.CreatePlayer(Stream);

            // handle the PlaybackEnded event
            Player.PlaybackEnded += Player_PlaybackEnded;

            // create a timer to report progress
            Timer = new System.Timers.Timer(50);

            Timer.Elapsed += async (state, args) =>
            {
                // calculate the percentage complete
                Percentage = (Player.CurrentPosition * 100) / Player.Duration;

                // calculate the PlayPosition string to report "current time / total time"
                var tsCurrent = TimeSpan.FromSeconds(Player.CurrentPosition);
                var tsTotal = TimeSpan.FromSeconds(Player.Duration);
                var durationString = $"{tsTotal.Minutes.ToString("D2")}:{tsTotal.Seconds.ToString("D2")}";
                var currentString = $"{tsCurrent.Minutes.ToString("D2")}:{tsCurrent.Seconds.ToString("D2")}";
                PlayPosition = $"{currentString} / {durationString}";
            };

            // start the timer
            Timer.Start();
            // start playing
            Player.Play();
            // configure the UI for playing
            SetState(MediaState.Playing);
            // update the UI
            await InvokeAsync(StateHasChanged);

        }
        catch (Exception e)
        {
            AudioMessage = "An error occurred. Please try again later.";
        }

    }

    /// <summary>
    /// Change UI depending on the state
    /// </summary>
    /// <param name="state"></param>
    public void SetState(MediaState state)
    {
        MediaState = state;
        if (state == MediaState.Playing)
        {
            ControlsOpacity = "1";
            PlayOpacity = ".5";
            AudioMessage = "Playing";
            PlayButtonClass = "";
            PauseButtonClass = "imageButton";
            StopButtonClass = "imageButton";
            RewindButtonClass = "imageButton";
            ForwardButtonClass = "imageButton";
        }
        else if (state == MediaState.Paused || state == MediaState.Stopped)
        {
            ControlsOpacity = ".5";
            PlayOpacity = "1";
            PlayButtonClass = "imageButton";
            PauseButtonClass = "";
            StopButtonClass = "";
            RewindButtonClass = "";
            ForwardButtonClass = "";
            if (state == MediaState.Stopped)
            {
                Percentage = 0;
                AudioMessage = "";
                PlayPosition = "";
            }
            else
            {
                AudioMessage = "Paused";
            }
        }
    }

    public void NavigateHome()
    {
        PlayingPlayList = false;
        _navigationManager.NavigateTo("/");
    }

    public async Task Cleanup()
    {
        SetState(MediaState.Stopped);
        // dispose the stream
        Stream?.Dispose();
        // stop and dispose the timer
        Timer?.Stop();
        Timer?.Dispose();
        // unhook this event 
        if (Player != null)
        {
            Player.PlaybackEnded -= Player_PlaybackEnded;
            // dispose the Player
            Player.Dispose();
        }
        // update the ui
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// When playing a playlist, sets ThisShow to the next show in the playlist
    /// It also loads the show details
    /// </summary>
    /// <returns></returns>
    private async Task GetNextShowInPlaylist()
    {
        // is this the last show in the playlist?
        if (ThisShow.ShowNumber == SelectedPlayList.Shows.Last().ShowNumber)
        {
            // This is a signal that we're done playing
            PlaylistShowIndex = 0;
            return;
        }
        // Get the next show from the playlist
        var nextShow = SelectedPlayList.Shows[PlaylistShowIndex];
        // Check the online status
        GetOnlineStatus();
        // Increment the show index (1-based index)
        PlaylistShowIndex++;
        // Get the show with the details
        ThisShow = await _apiService.GetShowWithDetails(nextShow.ShowNumber);
    }

    /// <summary>
    /// Tear down everything when playback ends
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Player_PlaybackEnded(object sender, EventArgs e)
    {
        await Cleanup();
        if (PlayingPlayList)
        {
            await GetNextShowInPlaylist();
            if (PlaylistShowIndex > 0)
                await PlayAudio();
            else
            {
                // go back home
                NavigateHome();
            }
        }
    }

    /// <summary>
    /// Used to display either "Add" or "Remove" buttons
    /// for playlist shows.
    /// </summary>
    /// <param name="show"></param>
    /// <returns></returns>
    public bool SelectedPlayListContainsShow(Show show)
    {
        var match = (from x in SelectedPlayList.Shows
                     where x.Id == show.Id
                     select x).FirstOrDefault();

        return (match != null);
    }

    /// <summary>
    /// Add every show in AllShows to the selected playlist
    /// </summary>
    public void AddAllToPlaylist()
    {
        if (SelectedPlayList == null || ShowPlayListOnly == true) return;
        foreach (var show in AllShows)
        {
            if (!SelectedPlayListContainsShow(show))
            {
                SelectedPlayList.Shows.Add(show);
            }
        }
        SavePlaylists();
    }

    /// <summary>
    /// Remove every show in AllShows from the selected playlsit
    /// </summary>
    public void RemoveAllFromPlaylist()
    {
        if (SelectedPlayList == null || ShowPlayListOnly == true) return;
        foreach (var show in AllShows)
        {
            if (SelectedPlayListContainsShow(show))
            {
                RemoveShowFromPlaylist(show);
            }
        }
    }

    /// <summary>
    /// Add a show to the selected playlist
    /// </summary>
    /// <param name="show"></param>
    public void AddShowToPlaylist(Show show)
    {
        if (SelectedPlayList == null) return;
        SelectedPlayList.Shows.Add(show);
        SavePlaylists();
    }

    /// <summary>
    /// Remove a show from the selected playlist
    /// </summary>
    /// <param name="show"></param>
    public void RemoveShowFromPlaylist(Show show)
    {
        if (SelectedPlayList == null) return;
        // the show objects may not be the same, so select by Id
        var match = (from x in SelectedPlayList.Shows
                     where x.Id == show.Id
                     select x).FirstOrDefault();
        if (match != null)
        {
            SelectedPlayList.Shows.Remove(match);
            SavePlaylists();
        }
    }

    /// <summary>
    /// Switches the list of shows in Main page to/from
    /// the shows in the selected playlist
    /// </summary>
    public void ToggleShowPlaylistOnly()
    {
        // Toggle
        ShowPlayListOnly = !ShowPlayListOnly;

        if (ShowPlayListOnly)
        {
            // Save the current state
            BackupEpisodeFilter = EpisodeFilter;    // filter
            AllShowsBackupString = JsonConvert.SerializeObject(AllShows);   // shows
            LastShowNumberBackup = LastShowNumber;  // last show number
                                                    // clear the filter
            EpisodeFilter = "";
            // change the set of displayed shows
            AllShows = new ObservableCollection<Show>(SelectedPlayList.Shows);
            // change the last show number
            LastShowNumber = 0;
            // change the button text
            ShowPlayListOnlyText = "Show All";
        }
        else
        {
            // restore state from backup values
            EpisodeFilter = BackupEpisodeFilter;
            AllShows = JsonConvert.DeserializeObject<ObservableCollection<Show>>(AllShowsBackupString);
            LastShowNumber = LastShowNumberBackup;
            // change the button text
            ShowPlayListOnlyText = "Show Playlist";
        }
    }

    /// <summary>
    /// Load the list of playlists from local Json file
    /// </summary>
    public void LoadPlaylists()
    {
        string cacheDir = FileSystem.Current.CacheDirectory;
        playListJsonFile = $"{cacheDir}\\playlists.json";
        try
        {
            if (System.IO.File.Exists(playListJsonFile))
            {
                string json = System.IO.File.ReadAllText(playListJsonFile);
                var list = JsonConvert.DeserializeObject<List<PlayList>>(json);
                PlayLists = new ObservableCollection<PlayList>(list);
            }
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// Save list of playlists to local Json file
    /// </summary>
    public void SavePlaylists()
    {
        if (playListJsonFile == "")
            LoadPlaylists();

        var list = PlayLists.ToList();
        try
        {
            var json = JsonConvert.SerializeObject(list);
            System.IO.File.WriteAllText(playListJsonFile, json);
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// Scoot ahead to the next episode in the playlist
    /// </summary>
    public void Scoot()
    {
        if (MediaState == MediaState.Playing)
        {
            // go almost to the end
            Player.Seek(Player.Duration - 3);
        }
    }

    /// <summary>
    /// We have to access Connectivity.Current.NetworkAccess
    /// on the main UI thread.
    /// </summary>
    public void GetOnlineStatus()
    {
        if (MainThread.IsMainThread)
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        else
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            });
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _apiService.ShuttingDown += _apiService_ShuttingDown;
        }
    }

    private async void _apiService_ShuttingDown(object sender, EventArgs e)
    {
        await Cleanup();
    }

}
