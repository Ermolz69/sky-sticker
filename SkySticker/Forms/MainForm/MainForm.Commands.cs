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
                item.IsPinned = false;
                _libraryService.Save(_imageItems);
                
                if (TryGetOverlay(item, out var overlay) && overlay != null)
                {
                    overlay.SetPinned(false);
                }
                
                RefreshListView();
                ShowDetails(item);
                _btnPin.Text = "📌 Pin / Open on Top";
            }
            else
            {
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
            var itemId = item.Id;
            
            if (TryGetOverlay(item, out var overlay) && overlay != null)
            {
                overlay.SetPinned(false);
            }
            
            item.IsPinned = false;
            _libraryService.Save(_imageItems);
            RefreshListView();
            
            foreach (ListViewItem lvItem in _listView.Items)
            {
                if (lvItem.Tag is ImageItem imgItem && imgItem.Id == itemId)
                {
                    lvItem.Selected = true;
                    lvItem.EnsureVisible();
                    break;
                }
            }
            
            if (_listView.SelectedItems.Count > 0 && _listView.SelectedItems[0].Tag is ImageItem selectedImgItem)
            {
                ShowDetails(selectedImgItem);
                _btnPin.Text = "📌 Pin / Open on Top";
            }
        }
    }
}

