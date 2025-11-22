using SkySticker.Models;

namespace SkySticker.Forms;

public partial class MainForm
{
    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
            Title = "Select Image",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            foreach (var filePath in openFileDialog.FileNames)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                var imageItem = new ImageItem
                {
                    Id = Guid.NewGuid(),
                    DisplayName = fileName,
                    FilePath = filePath,
                    Opacity = 100,
                    AlwaysOnTop = true,
                    LastUsed = DateTime.Now
                };

                _imageItems.Add(imageItem);
            }

            _libraryService.Save(_imageItems);
            RefreshListView();
        }
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var selectedItem = _listView.SelectedItems[0];
        if (selectedItem.Tag is ImageItem item)
        {
            if (MessageBox.Show($"Remove '{item.DisplayName}' from library?", "Remove",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _imageItems.Remove(item);
                _libraryService.Save(_imageItems);
                RefreshListView();
            }
        }
    }

    private void BtnPin_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var selectedItem = _listView.SelectedItems[0];
        if (selectedItem.Tag is ImageItem item)
        {
            if (item.IsPinned)
            {
                // Unpin
                item.IsPinned = false;
                _libraryService.Save(_imageItems);
                
                // Update OverlayForm if it's open
                if (TryGetOverlay(item, out var overlay) && overlay != null)
                {
                    overlay.SetPinned(false);
                }
                
                RefreshListView();
                ShowDetails(item);
                // Update Pin button text
                _btnPin.Text = "📌 Pin / Open on Top";
            }
            else
            {
                // Open/Pin
                OpenOverlay(item);
            }
        }
    }
    
    private void BtnUnpin_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var selectedItem = _listView.SelectedItems[0];
        if (selectedItem.Tag is ImageItem item && item.IsPinned)
        {
            // Save the item ID to restore selection after refresh
            var itemId = item.Id;
            
            // Update OverlayForm FIRST if it's open (before changing item.IsPinned)
            if (TryGetOverlay(item, out var overlay) && overlay != null)
            {
                overlay.SetPinned(false);
            }
            
            // Unpin the item
            item.IsPinned = false;
            _libraryService.Save(_imageItems);
            
            // Refresh the list view
            RefreshListView();
            
            // Restore selection after refresh
            foreach (ListViewItem lvItem in _listView.Items)
            {
                if (lvItem.Tag is ImageItem imgItem && imgItem.Id == itemId)
                {
                    lvItem.Selected = true;
                    lvItem.EnsureVisible();
                    break;
                }
            }
            
            // Update UI - this will also hide the Unpin button since item is no longer pinned
            if (_listView.SelectedItems.Count > 0 && _listView.SelectedItems[0].Tag is ImageItem selectedImgItem)
            {
                ShowDetails(selectedImgItem);
                // Update Pin button text
                _btnPin.Text = "📌 Pin / Open on Top";
            }
        }
    }
}

