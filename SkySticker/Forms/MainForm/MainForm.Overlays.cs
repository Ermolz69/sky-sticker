using SkySticker.Models;

namespace SkySticker.Forms;

public partial class MainForm
{
    private readonly Dictionary<Guid, OverlayForm> _openOverlays = new();

    protected void OpenOverlay(ImageItem item)
    {
        item.LastUsed = DateTime.Now;
        _libraryService.Save(_imageItems);
        
        if (TryGetOverlay(item, out var existingOverlay) && existingOverlay != null)
        {
            existingOverlay.Activate();
            existingOverlay.BringToFront();
            return;
        }
        
        var overlay = new OverlayForm(item, _libraryService, _imageItems);
        overlay.FormClosed += (s, e) => _openOverlays.Remove(item.Id);
        overlay.Show();
        _openOverlays[item.Id] = overlay;
        
        if (_listView.SelectedItems.Count > 0 && _listView.SelectedItems[0].Tag is ImageItem selectedItem && selectedItem.Id == item.Id)
        {
            ShowDetails(item);
        }
    }

    protected bool TryGetOverlay(ImageItem item, out OverlayForm? overlay)
    {
        if (_openOverlays.TryGetValue(item.Id, out var existingOverlay))
        {
            if (!existingOverlay.IsDisposed)
            {
                overlay = existingOverlay;
                return true;
            }
            else
            {
                _openOverlays.Remove(item.Id);
            }
        }
        
        overlay = null;
        return false;
    }

    protected void CloseAllOverlays()
    {
        foreach (var overlay in _openOverlays.Values.ToList())
        {
            if (!overlay.IsDisposed)
            {
                overlay.Close();
            }
        }
        _openOverlays.Clear();
    }
}

