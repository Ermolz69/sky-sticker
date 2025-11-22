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
    protected bool _isResizing = false;
    protected Button? _settingsButton;
    protected ContextMenuStrip? _contextMenu;
    protected System.Windows.Forms.Timer? _fadeInTimer;
    protected float _fadeOpacity = 0f;
    protected System.Windows.Forms.Timer? _saveTimer;
    protected System.Drawing.Imaging.ImageAttributes? _cachedImageAttributes;
    protected DateTime _lastInvalidateTime = DateTime.MinValue;
    protected const int InvalidateThrottleMs = 16; // ~60 FPS
    
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

        this.Text = _imageItem.DisplayName;
        this.FormBorderStyle = FormBorderStyle.None;
        // RGB(1,0,1) becomes transparent via COLORKEY
        this.BackColor = Color.FromArgb(1, 0, 1);
        this.TransparencyKey = Color.FromArgb(1, 0, 1);
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer, true);
        this.UpdateStyles();
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

        this.Load += OverlayForm_Load;
        this.MouseDown += OverlayForm_MouseDown;
        this.MouseMove += OverlayForm_MouseMove;
        this.MouseUp += OverlayForm_MouseUp;
        this.MouseEnter += OverlayForm_MouseEnter;
        this.MouseLeave += OverlayForm_MouseLeave;
        this.Move += OverlayForm_Move;
        this.SizeChanged += OverlayForm_SizeChanged;
        this.MouseClick += OverlayForm_MouseClick;
        this.KeyDown += OverlayForm_KeyDown;
        this.KeyPreview = true;

        CreateSettingsButton();
        CreateContextMenu();
        UpdateTopMost();

        this.ResumeLayout(false);
    }

    private async void LoadImage()
    {
        try
        {
            if (File.Exists(_imageItem.FilePath))
            {
                _originalImage?.Dispose();
                _originalImage = await Task.Run(() => Image.FromFile(_imageItem.FilePath));
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
        WinApiHelper.MakeTransparent(this.Handle);
        uint transparentKey = 0x00010001; // RGB(1,0,1) in BGR format
        var opacity = (byte)(_imageItem.Opacity * 255 / 100);
        WinApiHelper.SetLayeredWindowAttributes(this.Handle, transparentKey, opacity, WinApiHelper.LWA_COLORKEY | WinApiHelper.LWA_ALPHA);
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
    }

    private void OverlayForm_Move(object? sender, EventArgs e)
    {
        // Position is saved only on drag/resize completion to avoid blocking UI thread
    }

    private void OverlayForm_SizeChanged(object? sender, EventArgs e)
    {
        if (_imageItem.IsPinned && this.WindowState == FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Normal;
            return;
        }
        else if (this.WindowState == FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Normal;
            return;
        }

        if (_settingsButton != null && _isHovered && !_imageItem.IsPinned)
        {
            _settingsButton.Location = new Point(this.Width - SettingsButtonSize - 5, 5);
        }

        if (_resizeHandleRects != null)
        {
            UpdateResizeHandles();
        }

        // Additional refresh during resize ensures synchronization if ResizeWindow's refresh didn't complete
        if (_isResizing)
        {
            this.Refresh();
        }
        else
        {
            ThrottledInvalidate();
        }
    }
    
    protected override void SetVisibleCore(bool value)
    {
        if (_imageItem.IsPinned && !value)
        {
            return;
        }
        base.SetVisibleCore(value);
    }

    protected void ScheduleSavePositionAndSize()
    {
        // Debounce: save only 500ms after last change
        _saveTimer?.Stop();
        _saveTimer?.Dispose();
        _saveTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _saveTimer.Tick += (s, e) =>
        {
            SavePositionAndSize();
            _saveTimer?.Stop();
            _saveTimer?.Dispose();
            _saveTimer = null;
        };
        _saveTimer.Start();
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

    protected void ThrottledInvalidate()
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastInvalidateTime).TotalMilliseconds;
        if (elapsed >= InvalidateThrottleMs)
        {
            this.Invalidate();
            _lastInvalidateTime = now;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _saveTimer?.Stop();
        _saveTimer?.Dispose();
        SavePositionAndSize();
        _fadeInTimer?.Stop();
        _fadeInTimer?.Dispose();
        _originalImage?.Dispose();
        _cachedImageAttributes?.Dispose();
        _cachedImageAttributes = null;
        base.OnFormClosing(e);
    }
}

