using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SkySticker.Models;
using SkySticker.Services;
using SkySticker.Helpers;

namespace SkySticker.Forms;

public partial class OverlayForm : Form
{
    // Core fields
    protected readonly ImageItem _imageItem;
    protected readonly ImageLibraryService _libraryService;
    protected readonly List<ImageItem> _imageItems;
    protected Image? _originalImage;
    
    // State fields
    protected bool _isHovered;
    protected bool _isDragging;
    protected Point _dragStartPoint;
    protected Point _formStartLocation;
    protected ResizeHandle? _activeResizeHandle;
    protected bool _isRotating;
    protected Point _rotationStartPoint;
    protected float _rotationStartAngle;
    protected Button? _settingsButton;
    protected ContextMenuStrip? _contextMenu;
    protected System.Windows.Forms.Timer? _fadeInTimer;
    protected float _fadeOpacity = 0f;
    
    // Constants
    protected const int ResizeHandleSize = 7;
    protected const int SettingsButtonSize = 24;
    protected const int BorderThickness = 2;

    protected enum ResizeHandle
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
        this.KeyDown += OverlayForm_KeyDown;
        this.KeyPreview = true; // Enable key events

        // Кнопка настроек
        CreateSettingsButton();
        CreateContextMenu();
        
        // Устанавливаем TopMost после создания всех компонентов
        UpdateTopMost();

        this.ResumeLayout(false);
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
                    $"Image file not found:\n{_imageItem.FilePath}\n\nRemove entry '{_imageItem.DisplayName}' from library?",
                    "File not found",
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
            MessageBox.Show($"Error loading image: {ex.Message}", "Error",
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

    protected void SavePositionAndSize()
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

    protected void UpdateTopMost()
    {
        this.TopMost = _imageItem.AlwaysOnTop || _imageItem.IsPinned;
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

