namespace Damselfly.Core.ScopedServices;

/// <summary>
///     Service to maintain state around the toolbars - such as whether
///     we show the folder list or not.
/// </summary>
public class ViewDataService
{
    private SideBarState sidebarState = new();

    public bool ShowFolderList => sidebarState.ShowFolderList;
    public bool ShowTags => sidebarState.ShowTags;
    public bool ShowMap => sidebarState.ShowMap;
    public bool ShowBasket => sidebarState.ShowBasket;
    public bool ShowExport => sidebarState.ShowExport;
    public bool ShowImageProps => sidebarState.ShowImageProps;
    public bool ShowSideBar => !sidebarState.HideSideBar;
    public event Action<SideBarState> SideBarStateChanged;

    protected void OnStateChanged(SideBarState state)
    {
        SideBarStateChanged?.Invoke(state);
    }

    public void SetSideBarState(SideBarState state)
    {
        if (!state.Equals(sidebarState))
        {
            sidebarState = state;
            OnStateChanged(state);
        }
    }
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class SideBarState
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public bool ShowFolderList { get; set; } = false;
        public bool ShowTags { get; set; } = false;
        public bool ShowMap { get; set; } = false;
        public bool ShowBasket { get; set; } = false;
        public bool ShowExport { get; set; } = false;
        public bool ShowImageProps { get; set; } = false;
        public bool HideSideBar { get; set; } = false;

        public override bool Equals(object? obj)
        {
            var other = obj as SideBarState;
            if (other != null)
                return ShowBasket == other.ShowBasket &&
                       ShowFolderList == other.ShowFolderList &&
                       ShowExport == other.ShowExport &&
                       ShowMap == other.ShowMap &&
                       ShowTags == other.ShowTags &&
                       ShowImageProps == other.ShowImageProps &&
                       HideSideBar == other.HideSideBar;

            return false;
        }
    }
}
