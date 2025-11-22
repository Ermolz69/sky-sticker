using SkySticker.Models;

namespace SkySticker.Forms;

public partial class MainForm
{
    private void LoadLibrary()
    {
        _imageItems = _libraryService.Load();
        RefreshListView();
    }

    private void RefreshListView()
    {
        var searchText = _searchBox.Text.ToLower();
        var filteredItems = _imageItems.Where(item =>
            string.IsNullOrEmpty(searchText) ||
            item.DisplayName.ToLower().Contains(searchText) ||
            item.FilePath.ToLower().Contains(searchText)
        ).OrderByDescending(item => item.LastUsed ?? DateTime.MinValue).ToList();

        var itemsToRemove = new List<ListViewItem>();
        foreach (ListViewItem lvItem in _listView.Items)
        {
            if (lvItem.Tag is ImageItem imgItem && !filteredItems.Contains(imgItem))
            {
                itemsToRemove.Add(lvItem);
            }
        }
        foreach (var item in itemsToRemove)
        {
            _listView.Items.Remove(item);
        }

        var existingItemIds = new HashSet<Guid>();
        foreach (ListViewItem lvItem in _listView.Items)
        {
            if (lvItem.Tag is ImageItem imgItem)
            {
                existingItemIds.Add(imgItem.Id);
            }
        }

        foreach (var item in filteredItems)
        {
            if (!existingItemIds.Contains(item.Id))
            {
                try
                {
                    Image? thumbnail = null;
                    
                    if (!_thumbnailCache.TryGetValue(item.Id, out thumbnail))
                    {
                        if (File.Exists(item.FilePath))
                        {
                            using var original = Image.FromFile(item.FilePath);
                            thumbnail = CreateThumbnail(original, 64, 64);
                            _thumbnailCache[item.Id] = thumbnail;
                        }
                        else
                        {
                            thumbnail = new Bitmap(64, 64);
                            using var g = Graphics.FromImage(thumbnail);
                            g.Clear(Color.LightGray);
                            g.DrawString("?", new Font("Arial", 24), Brushes.Gray, new PointF(20, 15));
                            _thumbnailCache[item.Id] = thumbnail;
                        }
                    }
                    
                    if (!_imageList.Images.ContainsKey(item.Id.ToString()))
                    {
                        _imageList.Images.Add(item.Id.ToString(), thumbnail);
                    }

                    var listItem = new ListViewItem(item.DisplayName, item.Id.ToString())
                    {
                        Tag = item
                    };
                    _listView.Items.Add(listItem);
                }
                catch
                {
                }
            }
        }
    }

    protected Image CreateThumbnail(Image original, int width, int height)
    {
        var thumbnail = new Bitmap(width, height);
        using var g = Graphics.FromImage(thumbnail);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.DrawImage(original, 0, 0, width, height);
        return thumbnail;
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        RefreshListView();
    }

    protected string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

