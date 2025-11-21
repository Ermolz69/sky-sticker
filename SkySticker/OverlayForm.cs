using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SkySticker;

public class OverlayForm : Form
{
    private readonly ImageItem _imageItem;
    private readonly ImageLibraryService _libraryService;
    private readonly List<ImageItem> _imageItems;
    private Image? _originalImage;
    private bool _isHovered;
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _formStartLocation;
    private ResizeHandle? _activeResizeHandle;
    private Button? _settingsButton;
    private ContextMenuStrip? _contextMenu;
    private System.Windows.Forms.Timer? _fadeInTimer;
    private float _fadeOpacity = 0f;
    private const int ResizeHandleSize = 7;
    private const int SettingsButtonSize = 24;
    private const int BorderThickness = 2;

    private enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right
    }

    public OverlayForm(ImageItem imageItem, ImageLibraryService libraryService, List<ImageItem> imageItems)
    {
        _imageItem = imageItem;
        _libraryService = libraryService;
        _imageItems = imageItems;
        InitializeComponent();
        LoadImage();
        StartFadeIn();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Форма без рамки, прозрачная
        this.Text = _imageItem.DisplayName;
        this.FormBorderStyle = FormBorderStyle.None;
        // Используем очень редкий цвет для прозрачности (RGB 1,0,1) - почти Magenta, но не совсем
        // Этот цвет будет полностью прозрачным через COLORKEY
        this.BackColor = Color.FromArgb(1, 0, 1);
        this.TransparencyKey = Color.FromArgb(1, 0, 1);
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        this.UpdateStyles();

        // Применяем сохранённые координаты и размер
        if (_imageItem.LastX.HasValue && _imageItem.LastY.HasValue)
        {
            this.Location = new Point(_imageItem.LastX.Value, _imageItem.LastY.Value);
        }
        else
        {
            this.Location = new Point(100, 100);
        }

        if (_imageItem.LastWidth.HasValue && _imageItem.LastHeight.HasValue)
        {
            this.Size = new Size(_imageItem.LastWidth.Value, _imageItem.LastHeight.Value);
        }
        else
        {
            this.Size = new Size(300, 300);
        }

        this.MinimumSize = new Size(50, 50);
        this.Opacity = _imageItem.Opacity / 100.0;

        // Используем WinAPI для правильной прозрачности
        this.Load += OverlayForm_Load;

        // Обработчики событий
        this.MouseDown += OverlayForm_MouseDown;
        this.MouseMove += OverlayForm_MouseMove;
        this.MouseUp += OverlayForm_MouseUp;
        this.MouseEnter += OverlayForm_MouseEnter;
        this.MouseLeave += OverlayForm_MouseLeave;
        this.Move += OverlayForm_Move;
        this.SizeChanged += OverlayForm_SizeChanged;
        this.MouseClick += OverlayForm_MouseClick;

        // Кнопка настроек
        CreateSettingsButton();
        CreateContextMenu();
        
        // Устанавливаем TopMost после создания всех компонентов
        UpdateTopMost();

        this.ResumeLayout(false);
    }

    private void CreateSettingsButton()
    {
        _settingsButton = new Button
        {
            Size = new Size(SettingsButtonSize, SettingsButtonSize),
            FlatStyle = FlatStyle.Flat,
            Text = "⚙",
            Font = new Font("Segoe UI Symbol", 10),
            BackColor = Color.FromArgb(200, 30, 30, 30),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Visible = false
        };
        _settingsButton.FlatAppearance.BorderSize = 0;
        _settingsButton.Click += SettingsButton_Click;
        this.Controls.Add(_settingsButton);
    }

    private void CreateContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        // Close
        var closeItem = new ToolStripMenuItem("Close");
        closeItem.Click += (s, e) => this.Close();
        _contextMenu.Items.Add(closeItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Opacity
        var opacityMenu = new ToolStripMenuItem("Opacity");
        var opacityValues = new[] { 100, 90, 80, 70, 50 };
        foreach (var value in opacityValues)
        {
            var item = new ToolStripMenuItem($"{value}%");
            item.Click += (s, e) => SetOpacity(value);
            if (_imageItem.Opacity == value)
                item.Checked = true;
            opacityMenu.DropDownItems.Add(item);
        }
        _contextMenu.Items.Add(opacityMenu);

        // Always on Top
        var alwaysOnTopItem = new ToolStripMenuItem("Always on Top");
        alwaysOnTopItem.Checked = _imageItem.AlwaysOnTop;
        alwaysOnTopItem.Click += (s, e) =>
        {
            _imageItem.AlwaysOnTop = !_imageItem.AlwaysOnTop;
            alwaysOnTopItem.Checked = _imageItem.AlwaysOnTop;
            _libraryService.Save(_imageItems);
            UpdateTopMost();
        };
        _contextMenu.Items.Add(alwaysOnTopItem);

        // Pin / Unpin
        var pinItem = new ToolStripMenuItem(_imageItem.IsPinned ? "Unpin" : "Pin");
        pinItem.Click += (s, e) =>
        {
            TogglePin();
        };
        _contextMenu.Items.Add(pinItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Reset Size
        var resetSizeItem = new ToolStripMenuItem("Reset Size");
        resetSizeItem.Click += (s, e) =>
        {
            if (_originalImage != null)
            {
                this.Size = _originalImage.Size;
                SavePositionAndSize();
            }
        };
        _contextMenu.Items.Add(resetSizeItem);

        // Reset Position
        var resetPositionItem = new ToolStripMenuItem("Reset Position");
        resetPositionItem.Click += (s, e) =>
        {
            this.Location = new Point(100, 100);
            SavePositionAndSize();
        };
        _contextMenu.Items.Add(resetPositionItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Delete from Library
        var deleteItem = new ToolStripMenuItem("Delete from Library");
        deleteItem.Click += (s, e) =>
        {
            if (MessageBox.Show($"Удалить '{_imageItem.DisplayName}' из библиотеки?", "Удаление",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _imageItems.Remove(_imageItem);
                _libraryService.Save(_imageItems);
                this.Close();
            }
        };
        _contextMenu.Items.Add(deleteItem);

        // Open containing folder
        var openFolderItem = new ToolStripMenuItem("Open containing folder");
        openFolderItem.Click += (s, e) =>
        {
            try
            {
                var folder = Path.GetDirectoryName(_imageItem.FilePath);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                }
            }
            catch { }
        };
        _contextMenu.Items.Add(openFolderItem);

        // Rename
        var renameItem = new ToolStripMenuItem("Rename");
        renameItem.Click += (s, e) =>
        {
            using var dialog = new RenameDialog(_imageItem.DisplayName);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _imageItem.DisplayName = dialog.NewName;
                _libraryService.Save(_imageItems);
                this.Text = _imageItem.DisplayName;
                this.Invalidate();
            }
        };
        _contextMenu.Items.Add(renameItem);
    }

    private void LoadImage()
    {
        try
        {
            if (File.Exists(_imageItem.FilePath))
            {
                _originalImage?.Dispose();
                _originalImage = Image.FromFile(_imageItem.FilePath);
                this.Invalidate();
            }
            else
            {
                var result = MessageBox.Show(
                    $"Файл изображения не найден:\n{_imageItem.FilePath}\n\nУдалить запись '{_imageItem.DisplayName}' из библиотеки?",
                    "Файл не найден",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _imageItems.Remove(_imageItem);
                    _libraryService.Save(_imageItems);
                }

                this.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
        }
    }

    private void StartFadeIn()
    {
        _fadeOpacity = 0f;
        _fadeInTimer = new System.Windows.Forms.Timer { Interval = 10 };
        _fadeInTimer.Tick += (s, e) =>
        {
            _fadeOpacity += 0.1f;
            if (_fadeOpacity >= 1f)
            {
                _fadeOpacity = 1f;
                _fadeInTimer?.Stop();
            }
            this.Invalidate();
        };
        _fadeInTimer.Start();
    }

    private void OverlayForm_Load(object? sender, EventArgs e)
    {
        // Используем WinAPI для правильной прозрачности
        // Используем COLORKEY для цвета RGB(1,0,1) - он будет полностью прозрачным
        WinApiHelper.MakeTransparent(this.Handle);
        // RGB(1,0,1) в формате BGR = 0x00010001
        uint transparentKey = 0x00010001;
        var opacity = (byte)(_imageItem.Opacity * 255 / 100);
        WinApiHelper.SetLayeredWindowAttributes(this.Handle, transparentKey, opacity, WinApiHelper.LWA_COLORKEY | WinApiHelper.LWA_ALPHA);
        
        // Устанавливаем click-through если изображение закреплено
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Рисуем фон редким цветом RGB(1,0,1) - он будет прозрачным через COLORKEY
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Очищаем область редким цветом RGB(1,0,1) - он станет прозрачным через COLORKEY
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));

        if (_originalImage == null) return;

        var g = e.Graphics;
        // Используем высокое качество для избежания артефактов на краях
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        // Важно: Half для правильной обработки пикселей без смещения
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.CompositingQuality = CompositingQuality.HighQuality;
        // Используем SourceOver для правильной обработки прозрачности PNG
        g.CompositingMode = CompositingMode.SourceOver;

        var clientRect = this.ClientRectangle;

        // Рисуем изображение с сохранением пропорций
        var imageRect = CalculateImageRect(clientRect, _originalImage.Size);
        
        // Применяем fade-in эффект
        var opacity = _fadeOpacity * (_imageItem.Opacity / 100.0);
        var imageAttributes = new ImageAttributes();
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, (float)opacity, 0},
            new float[] {0, 0, 0, 0, 1}
        });
        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        // Важно: отключаем обрезку для правильной обработки краев
        imageAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);

        // Рисуем изображение с правильной обработкой прозрачности PNG
        // Фон уже очищен прозрачным цветом RGB(1,0,1), который станет прозрачным через COLORKEY
        g.DrawImage(
            _originalImage, 
            imageRect, 
            0, 0, 
            _originalImage.Width, 
            _originalImage.Height, 
            GraphicsUnit.Pixel, 
            imageAttributes);

        // Рисуем рамку при наведении
        if (_isHovered)
        {
            using var borderPen = new Pen(Color.FromArgb(68, 255, 255, 255), BorderThickness);
            g.DrawRectangle(borderPen, clientRect.X + BorderThickness / 2, clientRect.Y + BorderThickness / 2,
                clientRect.Width - BorderThickness, clientRect.Height - BorderThickness);
        }

        // Рисуем resize handles при наведении
        if (_isHovered)
        {
            DrawResizeHandles(g, clientRect);
        }

        imageAttributes.Dispose();
    }

    private Rectangle CalculateImageRect(Rectangle clientRect, Size imageSize)
    {
        var imageAspect = (float)imageSize.Width / imageSize.Height;
        var clientAspect = (float)clientRect.Width / clientRect.Height;

        int width, height;
        if (imageAspect > clientAspect)
        {
            width = clientRect.Width;
            height = (int)(width / imageAspect);
        }
        else
        {
            height = clientRect.Height;
            width = (int)(height * imageAspect);
        }

        var x = clientRect.X + (clientRect.Width - width) / 2;
        var y = clientRect.Y + (clientRect.Height - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    private void DrawResizeHandles(Graphics g, Rectangle rect)
    {
        using var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        var size = ResizeHandleSize;

        // Углы
        g.FillRectangle(brush, rect.Left, rect.Top, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Bottom - size, size, size);

        // Стороны
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Left, rect.Top + rect.Height / 2 - size / 2, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top + rect.Height / 2 - size / 2, size, size);
    }

    private ResizeHandle GetResizeHandle(Point mousePos)
    {
        var rect = this.ClientRectangle;
        var size = ResizeHandleSize;

        if (new Rectangle(rect.Left, rect.Top, size, size).Contains(mousePos))
            return ResizeHandle.TopLeft;
        if (new Rectangle(rect.Right - size, rect.Top, size, size).Contains(mousePos))
            return ResizeHandle.TopRight;
        if (new Rectangle(rect.Left, rect.Bottom - size, size, size).Contains(mousePos))
            return ResizeHandle.BottomLeft;
        if (new Rectangle(rect.Right - size, rect.Bottom - size, size, size).Contains(mousePos))
            return ResizeHandle.BottomRight;
        if (new Rectangle(rect.Left + rect.Width / 2 - size / 2, rect.Top, size, size).Contains(mousePos))
            return ResizeHandle.Top;
        if (new Rectangle(rect.Left + rect.Width / 2 - size / 2, rect.Bottom - size, size, size).Contains(mousePos))
            return ResizeHandle.Bottom;
        if (new Rectangle(rect.Left, rect.Top + rect.Height / 2 - size / 2, size, size).Contains(mousePos))
            return ResizeHandle.Left;
        if (new Rectangle(rect.Right - size, rect.Top + rect.Height / 2 - size / 2, size, size).Contains(mousePos))
            return ResizeHandle.Right;

        return ResizeHandle.None;
    }

    private void OverlayForm_MouseEnter(object? sender, EventArgs e)
    {
        // Если изображение закреплено, не показываем интерактивные элементы
        if (_imageItem.IsPinned)
            return;
            
        _isHovered = true;
        if (_settingsButton != null)
        {
            _settingsButton.Location = new Point(this.Width - SettingsButtonSize - 5, 5);
            _settingsButton.Visible = true;
        }
        this.Invalidate();
    }

    private void OverlayForm_MouseLeave(object? sender, EventArgs e)
    {
        if (!this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
        {
            _isHovered = false;
            if (_settingsButton != null)
                _settingsButton.Visible = false;
            this.Invalidate();
        }
    }

    private void OverlayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        // Если изображение закреплено, блокируем все интеракции
        if (_imageItem.IsPinned)
            return;
            
        if (e.Button == MouseButtons.Left)
        {
            _activeResizeHandle = GetResizeHandle(e.Location);
            if (_activeResizeHandle == ResizeHandle.None)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
                _formStartLocation = this.Location;
                WinApiHelper.MoveWindow(this.Handle);
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            _contextMenu?.Show(this, e.Location);
        }
    }

    private void OverlayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        // Если изображение закреплено, блокируем все интеракции
        if (_imageItem.IsPinned)
        {
            this.Cursor = Cursors.Default;
            return;
        }
        
        if (_isDragging && e.Button == MouseButtons.Left)
        {
            // Перемещение обрабатывается через WinAPI
        }
        else if (_activeResizeHandle != ResizeHandle.None && e.Button == MouseButtons.Left)
        {
            ResizeWindow(e.Location);
        }
        else
        {
            var handle = GetResizeHandle(e.Location);
            if (handle != ResizeHandle.None)
            {
                this.Cursor = GetCursorForHandle(handle);
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }
    }

    private Cursor GetCursorForHandle(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
            ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
            ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
            _ => Cursors.Default
        };
    }

    private void ResizeWindow(Point mousePos)
    {
        if (_activeResizeHandle == ResizeHandle.None) return;

        var deltaX = mousePos.X - _dragStartPoint.X;
        var deltaY = mousePos.Y - _dragStartPoint.Y;
        var newLocation = this.Location;
        var newSize = this.Size;

        switch (_activeResizeHandle)
        {
            case ResizeHandle.TopLeft:
                newLocation = new Point(this.Location.X + deltaX, this.Location.Y + deltaY);
                newSize = new Size(this.Width - deltaX, this.Height - deltaY);
                break;
            case ResizeHandle.TopRight:
                newLocation = new Point(this.Location.X, this.Location.Y + deltaY);
                newSize = new Size(this.Width + deltaX, this.Height - deltaY);
                break;
            case ResizeHandle.BottomLeft:
                newLocation = new Point(this.Location.X + deltaX, this.Location.Y);
                newSize = new Size(this.Width - deltaX, this.Height + deltaY);
                break;
            case ResizeHandle.BottomRight:
                newSize = new Size(this.Width + deltaX, this.Height + deltaY);
                break;
            case ResizeHandle.Top:
                newLocation = new Point(this.Location.X, this.Location.Y + deltaY);
                newSize = new Size(this.Width, this.Height - deltaY);
                break;
            case ResizeHandle.Bottom:
                newSize = new Size(this.Width, this.Height + deltaY);
                break;
            case ResizeHandle.Left:
                newLocation = new Point(this.Location.X + deltaX, this.Location.Y);
                newSize = new Size(this.Width - deltaX, this.Height);
                break;
            case ResizeHandle.Right:
                newSize = new Size(this.Width + deltaX, this.Height);
                break;
        }

        if (newSize.Width >= this.MinimumSize.Width && newSize.Height >= this.MinimumSize.Height)
        {
            this.Location = newLocation;
            this.Size = newSize;
            _dragStartPoint = mousePos;
        }
    }

    private void OverlayForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            _activeResizeHandle = ResizeHandle.None;
        }
    }

    private void OverlayForm_MouseClick(object? sender, MouseEventArgs e)
    {
        // Если изображение закреплено, блокируем контекстное меню
        if (_imageItem.IsPinned)
            return;
            
        if (e.Button == MouseButtons.Right)
        {
            _contextMenu?.Show(this, e.Location);
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        if (_settingsButton != null && _contextMenu != null)
        {
            _contextMenu.Show(_settingsButton, new Point(0, _settingsButton.Height));
        }
    }

    private void SetOpacity(int opacity)
    {
        _imageItem.Opacity = opacity;
        this.Opacity = opacity / 100.0;
        // Обновляем альфа-канал через WinAPI с COLORKEY
        uint transparentKey = 0x00010001; // RGB(1,0,1) в формате BGR
        var alpha = (byte)(opacity * 255 / 100);
        WinApiHelper.SetLayeredWindowAttributes(this.Handle, transparentKey, alpha, WinApiHelper.LWA_COLORKEY | WinApiHelper.LWA_ALPHA);
        _libraryService.Save(_imageItems);
    }

    private void OverlayForm_Move(object? sender, EventArgs e)
    {
        SavePositionAndSize();
    }

    private void OverlayForm_SizeChanged(object? sender, EventArgs e)
    {
        // Если изображение закреплено, не позволяем сворачивать окно
        if (_imageItem.IsPinned && this.WindowState == FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Normal;
        }
        else if (this.WindowState == FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Normal;
        }

        // Если изображение закреплено, не показываем кнопку настроек
        if (_settingsButton != null && _isHovered && !_imageItem.IsPinned)
        {
            _settingsButton.Location = new Point(this.Width - SettingsButtonSize - 5, 5);
        }

        // Принудительная перерисовка при изменении размера
        this.Invalidate(true);
        this.Update();
        
        SavePositionAndSize();
    }
    
    protected override void SetVisibleCore(bool value)
    {
        // Если изображение закреплено, не позволяем скрывать окно
        if (_imageItem.IsPinned && !value)
        {
            return; // Игнорируем попытку скрыть окно
        }
        base.SetVisibleCore(value);
    }

    private void SavePositionAndSize()
    {
        if (this.WindowState == FormWindowState.Normal)
        {
            _imageItem.LastX = this.Location.X;
            _imageItem.LastY = this.Location.Y;
            _imageItem.LastWidth = this.Width;
            _imageItem.LastHeight = this.Height;
            _imageItem.LastUsed = DateTime.Now;
            _libraryService.Save(_imageItems);
        }
    }

    private void UpdateTopMost()
    {
        this.TopMost = _imageItem.AlwaysOnTop || _imageItem.IsPinned;
    }
    
    private bool _temporarilyLowered = false;
    
    public bool IsPinned()
    {
        return _imageItem.IsPinned;
    }
    
    public void TemporarilyLowerTopMost()
    {
        // Временно убираем TopMost только для pinned стикеров
        if (_imageItem.IsPinned && this.TopMost && !_temporarilyLowered)
        {
            this.TopMost = false;
            _temporarilyLowered = true;
        }
    }
    
    public void RestoreTopMost()
    {
        // Восстанавливаем TopMost только если он был временно опущен
        if (_temporarilyLowered)
        {
            UpdateTopMost();
            _temporarilyLowered = false;
        }
    }

    public void TogglePin()
    {
        _imageItem.IsPinned = !_imageItem.IsPinned;
        _libraryService.Save(_imageItems);
        
        // Устанавливаем click-through режим (клики проходят сквозь окно)
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
        UpdateTopMost();
        
        // Если закрепили, скрываем интерактивные элементы
        if (_imageItem.IsPinned)
        {
            _isHovered = false;
            if (_settingsButton != null)
                _settingsButton.Visible = false;
        }
        this.Invalidate();
    }
    
    public void SetPinned(bool pinned)
    {
        if (_imageItem.IsPinned != pinned)
        {
            _imageItem.IsPinned = pinned;
            _libraryService.Save(_imageItems);
            
            // Устанавливаем click-through режим (клики проходят сквозь окно)
            WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
            UpdateTopMost();
            
            // Если закрепили, скрываем интерактивные элементы
            if (_imageItem.IsPinned)
            {
                _isHovered = false;
                if (_settingsButton != null)
                    _settingsButton.Visible = false;
            }
            this.Invalidate();
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;

        // Когда стикер закреплён – он прозрачный для мыши на этапе хит-теста
        if (_imageItem.IsPinned && m.Msg == WM_NCHITTEST)
        {
            // Говорим системе: "я прозрачный, ищи окно ниже"
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SavePositionAndSize();
        _fadeInTimer?.Stop();
        _fadeInTimer?.Dispose();
        _originalImage?.Dispose();
        base.OnFormClosing(e);
    }
}

// Простой диалог для переименования
public class RenameDialog : Form
{
    private TextBox _textBox = null!;
    private string _newName = "";
    public string NewName => _newName;

    public RenameDialog(string currentName)
    {
        this.Text = "Rename";
        this.Size = new Size(300, 120);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        _textBox = new TextBox
        {
            Text = currentName,
            Location = new Point(12, 12),
            Size = new Size(260, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(116, 50),
            Size = new Size(75, 23)
        };
        btnOk.Click += (s, e) =>
        {
            _newName = _textBox.Text;
            this.DialogResult = DialogResult.OK;
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(197, 50),
            Size = new Size(75, 23)
        };

        this.Controls.Add(_textBox);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}
