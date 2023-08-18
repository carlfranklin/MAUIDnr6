using Blazored.Modal;
using Blazored.Modal.Services;

namespace MAUIDnr1.Pages;
public partial class Playlists : ComponentBase
{
    [Inject]
    private NavigationManager _navigationManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [CascadingParameter]
    public CascadingAppState AppState { get; set; }

    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    // are we adding, editing, or neither?
    protected PlaylistEditAction PlaylistEditAction { get; set; }

    // used to disable the command buttons if we're adding or editing
    protected bool CommandButtonsDisabled =>
        PlaylistEditAction != PlaylistEditAction.None;

    // This is the PlayList object we use to add or edit
    protected PlayList PlayListToAddOrEdit;

    protected List<CustomListItem> PlayListItems
    {
        get
        {
            var items = new List<CustomListItem>();
            foreach (var playList in AppState.PlayLists)
            {
                items.Add(new CustomListItem
                {
                    Text = playList.Name,
                    Value = playList.Id.ToString(),
                    Selected = AppState.SelectedPlayList?.Id == playList.Id
                });
            }
            return items;
        }
    }

    /// <summary>
    /// Called from the UI to move a show up in the playlist order
    /// </summary>
    /// <param name="show"></param>
    protected void MoveUp(Show show)
    {
        var index = AppState.SelectedPlayList.Shows.IndexOf(show);
        AppState.SelectedPlayList.Shows.RemoveAt(index);
        AppState.SelectedPlayList.Shows.Insert(index - 1, show);
    }

    /// <summary>
    /// Called from the UI to move a show down in the playlist order
    /// </summary>
    /// <param name="show"></param>
    protected void MoveDown(Show show)
    {
        var index = AppState.SelectedPlayList.Shows.IndexOf(show);
        AppState.SelectedPlayList.Shows.RemoveAt(index);
        AppState.SelectedPlayList.Shows.Insert(index + 1, show);
    }

    /// <summary>
    /// Go back
    /// </summary>
    protected void NavigateHome()
    {
        _navigationManager.NavigateTo("/");
    }

    /// <summary>
    /// Set the selected playlist when selected from the <select> element
    /// </summary>
    /// <param name="args"></param>
    protected void PlayListSelected(CustomListItem item)
    {
        AppState.SelectedPlayList = (from x in AppState.PlayLists
                                     where x.Id.ToString() == item.Value.ToString()
                                     select x).FirstOrDefault();
        if (AppState.ShowPlayListOnly)
        {
            AppState.ToggleShowPlaylistOnly();
        }
    }

    /// <summary>
    /// Because PlayListSelected won't fire when there is only one item in the list
    /// </summary>
    protected void PlayListsClicked()
    {
        if (AppState.PlayLists.Count == 1)
        {
            AppState.SelectedPlayList = AppState.PlayLists.First();
            if (AppState.ShowPlayListOnly)
            {
                AppState.ToggleShowPlaylistOnly();
            }
        }
    }

    protected override void OnInitialized()
    {
        if (AppState.PlayLists != null
            && AppState.SelectedPlayList == null
            && AppState.PlayLists.Count > 0)
        {
            AppState.SelectedPlayList = AppState.PlayLists.First();
        }
    }

    /// <summary>
    /// Add a PlayList
    /// </summary>
    protected async Task AddButtonClicked()
    {
        // Create a new PlayList
        PlayListToAddOrEdit = new PlayList();
        PlayListToAddOrEdit.Id = PlayList.CreateGuid(); // don't forget this!
        PlayListToAddOrEdit.DateCreated = DateTime.Now;
        PlaylistEditAction = PlaylistEditAction.Adding;
        //await JSRuntime.InvokeVoidAsync("SetFocus", "InputName");
        var options = new ModalOptions()
        {
            Position = ModalPosition.Middle,
            HideCloseButton = true
        };
        
        var popupEditor = Modal.Show<PopupEditor>("Playlist Name:", options);
        var result = await popupEditor.Result;
        if (result.Confirmed)
        {
            var playlistName = result.Data.ToString();
            // Do we already have a playlist with this name?
            var existing = (from x in AppState.PlayLists
                            where x.Name.ToLower().Trim() == playlistName.ToLower().Trim()
                            select x).FirstOrDefault();
            if (existing == null)
            {
                PlayListToAddOrEdit.Name = result.Data.ToString();
                // Simply add the new PlayList.
                AppState.PlayLists.Add(PlayListToAddOrEdit);
                // Select it
                int index = AppState.PlayLists.IndexOf(PlayListToAddOrEdit);
                AppState.SelectedPlayList = AppState.PlayLists[index];
                AppState.SavePlaylists();
            }
            else
            {
                var msgModal = Modal.Show<MessageBox>($"Playlist {playlistName} already exists", options);
                await msgModal.Result;
            }
        }

        PlaylistEditAction = PlaylistEditAction.None;
    }

    /// <summary>
    /// Edit the SelectedPlayList
    /// </summary>
    protected async Task EditButtonClicked()
    {
        // Clone it, so we don't clobber it accidentally.
        PlayListToAddOrEdit = (PlayList)AppState.SelectedPlayList.Clone();
        PlaylistEditAction = PlaylistEditAction.Editing;
        //await JSRuntime.InvokeVoidAsync("SetFocus", "InputName");
        var parameters = new ModalParameters()
            .Add(nameof(PopupEditor.Value), PlayListToAddOrEdit.Name);
        var options = new ModalOptions()
        {
            Position = ModalPosition.Middle,
            DisableBackgroundCancel = true,
            HideCloseButton = true
        };
        var popupEditor = Modal.Show<PopupEditor>("Playlist Name:", parameters, options);
        var result = await popupEditor.Result;
        if (result.Confirmed)
        {
            var playListName = result.Data.ToString().Trim();

            if (playListName.ToLower() == PlayListToAddOrEdit.Name.ToLower())
                return;

            // Do we already have another playlist with this name?
            var existing = (from x in AppState.PlayLists
                            where x.Id != PlayListToAddOrEdit.Id
                            && x.Name.ToLower().Trim() == playListName.ToLower().Trim()
                            select x).FirstOrDefault();
            if (existing == null)
            {
                PlayListToAddOrEdit.Name = result.Data.ToString();
                // Get the index of the selected play list
                int index = AppState.PlayLists.IndexOf(AppState.SelectedPlayList);
                // Replace it in the list
                AppState.PlayLists[index] = PlayListToAddOrEdit;
                // Get the new object reference
                AppState.SelectedPlayList = AppState.PlayLists[index];
                AppState.SavePlaylists();
            }
            else
            {
                var msgModal = Modal.Show<MessageBox>($"Playlist {playListName} already exists", options);
                await msgModal.Result;
            }
        }
        PlaylistEditAction = PlaylistEditAction.None;
    }

    protected void DeselectPlaylist()
    {
        AppState.SelectedPlayList = null;
    }

    /// <summary>
    /// Easy Peasy
    /// </summary>
    protected void DeleteButtonClicked()
    {
        AppState.PlayLists.Remove(AppState.SelectedPlayList);
        AppState.SavePlaylists();
        AppState.SelectedPlayList = null;
        PlaylistEditAction = PlaylistEditAction.None;
        if (AppState.PlayLists.Count == 1)
        {
            AppState.SelectedPlayList = AppState.PlayLists.First();
        }
        if (AppState.ShowPlayListOnly)
        {
            AppState.ToggleShowPlaylistOnly();
        }
    }

    /// <summary>
    /// Easy Peasy
    /// </summary>
    protected void CancelButtonPressed()
    {
        PlayListToAddOrEdit = null;
        PlaylistEditAction = PlaylistEditAction.None;
    }
}