namespace SkySticker;

public class MainForm : Form
{
    private readonly ImageLibraryService _libraryService;
    private List<ImageItem> _imageItems;
    private ListView _listView = null!;
    private ImageList _imageList = null!;
    private TextBox _searchBox = null!;
    private Button _btnAdd = null!;
    private Button _btnRemove = null!;
    private Button _btnPin = null!;
    private Panel _detailsPanel = null!;
    private PictureBox _previewBox = null!;
    private Label _detailsLabel = null!;

    public MainForm()
    {
        _libraryService = new ImageLibraryService();
        _imageItems = new List<ImageItem>();
        InitializeComponent();
        LoadLibrary();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // ImageList для превью
        _imageList = new ImageList
        {
            ImageSize = new Size(64, 64),
            ColorDepth = ColorDepth.Depth32Bit
        };

        // Search Box - только слева, не растягивается
        _searchBox = new TextBox
        {
            Location = new Point(12, 12),
            Size = new Size(400, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            PlaceholderText = "Поиск изображений..."
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

        // ListView - слева, фиксированная ширина, не растягивается вправо
        _listView = new ListView
        {
            Location = new Point(12, 45),
            Size = new Size(400, 380),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            View = View.LargeIcon,
            LargeImageList = _imageList,
            MultiSelect = false,
            FullRowSelect = false
        };
        _listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
        _listView.DoubleClick += ListView_DoubleClick;

        // Details Panel - справа, фиксированная ширина, на одном уровне с ListView
        _detailsPanel = new Panel
        {
            Location = new Point(428, 45), // 12 + 400 + 16 (отступ между ListView и DetailsPanel)
            Size = new Size(250, 380),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        _previewBox = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(230, 200),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightGray
        };

        _detailsLabel = new Label
        {
            Location = new Point(10, 220),
            Size = new Size(230, 170),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = false
        };

        _detailsPanel.Controls.Add(_previewBox);
        _detailsPanel.Controls.Add(_detailsLabel);

        // Bottom Panel for buttons
        var bottomPanel = new Panel
        {
            Height = 40,                      
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(10, 4, 10, 4)
        };

        const int btnHeight = 30;
        
        // Buttons
        _btnAdd = new Button
        {
            Text = "➕ Add",
            Size = new Size(110, btnHeight),   // было 130 x 40
            Location = new Point(10, 5),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        _btnAdd.FlatAppearance.BorderSize = 0;
        _btnAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 100, 195);
        _btnAdd.Click += BtnAdd_Click;

        _btnRemove = new Button
        {
            Text = "🗑 Remove",
            Size = new Size(110, btnHeight),
            Location = new Point(130, 5),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Enabled = false,
            BackColor = Color.FromArgb(196, 43, 28),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        _btnRemove.FlatAppearance.BorderSize = 0;
        _btnRemove.FlatAppearance.MouseOverBackColor = Color.FromArgb(176, 23, 8);
        _btnRemove.Click += BtnRemove_Click;

        _btnPin = new Button
        {
            Text = "📌 Pin / Открыть поверх",
            Size = new Size(200, btnHeight),
            Location = new Point(250, 5),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Enabled = false,
            BackColor = Color.FromArgb(16, 124, 16),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        _btnPin.FlatAppearance.BorderSize = 0;
        _btnPin.FlatAppearance.MouseOverBackColor = Color.FromArgb(6, 104, 6);
        _btnPin.Click += BtnPin_Click;

        // Layout buttons in bottom panel
        bottomPanel.Controls.Add(_btnAdd);
        bottomPanel.Controls.Add(_btnRemove);
        bottomPanel.Controls.Add(_btnPin);

        // MainForm
        this.Text = "SkySticker - Библиотека изображений";
        this.Size = new Size(690, 510);
        // Минимальный размер: 12 (отступ слева) + 400 (ListView) + 16 (отступ) + 250 (DetailsPanel) + 12 (отступ справа) = 690
        this.MinimumSize = new Size(690, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(243, 243, 243);
        
        // Обработчик изменения размера для корректного позиционирования
        this.Resize += MainForm_Resize;

        this.Controls.Add(_searchBox);
        this.Controls.Add(_listView);
        this.Controls.Add(_detailsPanel);
        this.Controls.Add(bottomPanel);

        this.ResumeLayout(false);
        
        // Вызываем Resize для правильного начального позиционирования
        this.Load += (s, e) => MainForm_Resize(s, e);
    }

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

    private Image CreateThumbnail(Image original, int width, int height)
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

    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool hasSelection = _listView.SelectedItems.Count > 0;
        _btnRemove.Enabled = hasSelection;
        _btnPin.Enabled = hasSelection;

        if (hasSelection && _listView.SelectedItems[0].Tag is ImageItem item)
        {
            ShowDetails(item);
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

    private void ShowDetails(ImageItem item)
    {
        try
        {
            if (File.Exists(item.FilePath))
            {
                using var original = Image.FromFile(item.FilePath);
                var preview = CreateThumbnail(original, 230, 200);
                _previewBox.Image?.Dispose();
                _previewBox.Image = preview;

                var fileInfo = new FileInfo(item.FilePath);
                var details = $"Имя: {item.DisplayName}\n\n" +
                             $"Разрешение: {original.Width} × {original.Height}\n" +
                             $"Размер файла: {FormatFileSize(fileInfo.Length)}\n" +
                             $"Путь: {item.FilePath}\n\n" +
                             $"Прозрачность: {item.Opacity}%\n" +
                             $"Всегда поверх: {(item.AlwaysOnTop ? "Да" : "Нет")}\n" +
                             $"Последнее использование: {(item.LastUsed?.ToString("g") ?? "Никогда")}";

                _detailsLabel.Text = details;
            }
            else
            {
                _previewBox.Image?.Dispose();
                _previewBox.Image = null;
                _detailsLabel.Text = $"Файл не найден:\n{item.FilePath}";
            }
        }
        catch (Exception ex)
        {
            _previewBox.Image?.Dispose();
            _previewBox.Image = null;
            _detailsLabel.Text = $"Ошибка загрузки:\n{ex.Message}";
        }
    }

    private void ClearDetails()
    {
        _previewBox.Image?.Dispose();
        _previewBox.Image = null;
        _detailsLabel.Text = "Выберите изображение для просмотра деталей";
    }

    private string FormatFileSize(long bytes)
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

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
            Title = "Выберите изображение",
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
            if (MessageBox.Show($"Удалить '{item.DisplayName}' из библиотеки?", "Удаление",
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
            OpenOverlay(item);
        }
    }

    private void OpenOverlay(ImageItem item)
    {
        item.LastUsed = DateTime.Now;
        _libraryService.Save(_imageItems);
        var overlay = new OverlayForm(item, _libraryService, _imageItems);
        overlay.Show();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        // Убеждаемся, что DetailsPanel не перекрывает ListView
        // ListView заканчивается на: 12 + 400 = 412
        // DetailsPanel должен начинаться не раньше: 412 + 16 = 428
        int listViewRight = _listView.Left + _listView.Width;
        int minDetailsPanelLeft = listViewRight + 16; // Минимум 16px отступ
        int detailsPanelWidth = 250;
        int rightMargin = 12;
        int currentDetailsPanelLeft = this.ClientSize.Width - detailsPanelWidth - rightMargin;
        
        // Если DetailsPanel перекрывает ListView, перемещаем его вправо
        if (currentDetailsPanelLeft < minDetailsPanelLeft)
        {
            _detailsPanel.Left = minDetailsPanelLeft;
        }
        else
        {
            // Иначе используем стандартное позиционирование от правого края
            _detailsPanel.Left = currentDetailsPanelLeft;
        }
        _detailsPanel.Width = detailsPanelWidth;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Очищаем ресурсы
        foreach (Image img in _imageList.Images)
        {
            img.Dispose();
        }
        _previewBox.Image?.Dispose();
        base.OnFormClosing(e);
    }
}
