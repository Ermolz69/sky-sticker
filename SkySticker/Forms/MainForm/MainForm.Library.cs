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
        _listView.Items.Clear();
        _imageList.Images.Clear();

        var searchText = _searchBox.Text.ToLower();
        var filteredItems = _imageItems.Where(item =>
            string.IsNullOrEmpty(searchText) ||
            item.DisplayName.ToLower().Contains(searchText) ||
            item.FilePath.ToLower().Contains(searchText)
        ).OrderByDescending(item => item.LastUsed ?? DateTime.MinValue).ToList();

        foreach (var item in filteredItems)
        {
            try
            {
                Image? thumbnail = null;
                if (File.Exists(item.FilePath))
                {
                    using var original = Image.FromFile(item.FilePath);
                    thumbnail = CreateThumbnail(original, 64, 64);
                    _imageList.Images.Add(item.Id.ToString(), thumbnail);
                }
                else
                {
                    // Placeholder для отсутствующих файлов
                    thumbnail = new Bitmap(64, 64);
                    using var g = Graphics.FromImage(thumbnail);
                    g.Clear(Color.LightGray);
                    g.DrawString("?", new Font("Arial", 24), Brushes.Gray, new PointF(20, 15));
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
                // Пропускаем проблемные изображения
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

