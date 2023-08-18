namespace MAUIDnr1.Shared;

public partial class CustomList : ComponentBase
{
    [Parameter]
    public EventCallback<CustomListItem> ItemSelected { get; set; }

    [Parameter]
    public string MaxHeight { get; set; } = "200px";

    [Parameter]
    public List<CustomListItem> Items { get; set; } = new List<CustomListItem>();

    protected void SelectItem(CustomListItem item)
    {
        foreach (var i in Items)
        {
            i.Selected = false;
        }
        item.Selected = true;
        OnItemSelected(item);
        StateHasChanged();
    }

    public virtual void OnItemSelected(CustomListItem item)
    {
        ItemSelected.InvokeAsync(item);
    }
}

public class CustomListItem
{
    public string Text { get; set; } = "";
    public object Value { get; set; }
    public bool Selected { get; set; } = false;
}

