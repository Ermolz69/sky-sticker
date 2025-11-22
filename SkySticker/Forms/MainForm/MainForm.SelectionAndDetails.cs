using SkySticker.Models;

namespace SkySticker.Forms;

public partial class MainForm
{
    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool hasSelection = _listView.SelectedItems.Count > 0;
        _btnRemove.Enabled = hasSelection;
        _btnPin.Enabled = hasSelection;

        if (hasSelection && _listView.SelectedItems[0].Tag is ImageItem item)
        {
            ShowDetails(item);
            _btnPin.Text = item.IsPinned ? "📌 Unpin" : "📌 Pin / Open on Top";
        }
        else
        {
            ClearDetails();
        }
    }

    private void ListView_DoubleClick(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count > 0 && _listView.SelectedItems[0].Tag is ImageItem item)
        {
            OpenOverlay(item);
        }
    }

    protected async void ShowDetails(ImageItem item)
    {
        try
        {
            if (File.Exists(item.FilePath))
            {
                var original = await Task.Run(() => Image.FromFile(item.FilePath));
                int width, height;
                using (original)
                {
                    var preview = CreateThumbnail(original, 230, 200);
                    _previewBox.Image?.Dispose();
                    _previewBox.Image = preview;
                    width = original.Width;
                    height = original.Height;
                }

                var fileInfo = new FileInfo(item.FilePath);
                var details = $"Name: {item.DisplayName}\n\n" +
                             $"Resolution: {width} × {height}\n" +
                             $"File size: {FormatFileSize(fileInfo.Length)}\n" +
                             $"Path: {item.FilePath}\n\n" +
                             $"Opacity: {item.Opacity}%\n" +
                             $"Always on top: {(item.AlwaysOnTop ? "Yes" : "No")}\n" +
                             $"Pinned: {(item.IsPinned ? "Yes" : "No")}\n" +
                             $"Last used: {(item.LastUsed?.ToString("g") ?? "Never")}";

                _detailsLabel.Text = details;
                _btnUnpin.Visible = item.IsPinned && TryGetOverlay(item, out _);
            }
            else
            {
                _previewBox.Image?.Dispose();
                _previewBox.Image = null;
                _detailsLabel.Text = $"File not found:\n{item.FilePath}";
            }
        }
        catch (Exception ex)
        {
            _previewBox.Image?.Dispose();
            _previewBox.Image = null;
            _detailsLabel.Text = $"Loading error:\n{ex.Message}";
        }
    }

    protected void ClearDetails()
    {
        _previewBox.Image?.Dispose();
        _previewBox.Image = null;
        _detailsLabel.Text = "Select an image to view details";
        _btnUnpin.Visible = false;
    }
}
