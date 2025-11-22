using SkySticker.Models;
using SkySticker.Services;
using SkySticker.Forms;

namespace SkySticker.Forms;

public partial class MainForm : Form
{
    protected readonly ImageLibraryService _libraryService;
    protected List<ImageItem> _imageItems;
    protected readonly Dictionary<Guid, Image> _thumbnailCache = new();
    protected ListView _listView = null!;
    protected ImageList _imageList = null!;
    protected TextBox _searchBox = null!;
    protected Button _btnAdd = null!;
    protected Button _btnRemove = null!;
    protected Button _btnPin = null!;
    protected Panel _detailsPanel = null!;
    protected PictureBox _previewBox = null!;
    protected Label _detailsLabel = null!;
    protected Button _btnUnpin = null!;

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

        _imageList = new ImageList
        {
            ImageSize = new Size(64, 64),
            ColorDepth = ColorDepth.Depth32Bit
        };

        _searchBox = new TextBox
        {
            Location = new Point(12, 12),
            Size = new Size(400, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            PlaceholderText = "Search images..."
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

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

        _detailsPanel = new Panel
        {
            Location = new Point(428, 45),
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
            Size = new Size(230, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = false
        };

        _btnUnpin = new Button
        {
            Text = "🔓 Unpin",
            Location = new Point(10, 350),
            Size = new Size(230, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Visible = false
        };
        _btnUnpin.FlatAppearance.BorderSize = 0;
        _btnUnpin.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 213, 0);
        _btnUnpin.Click += BtnUnpin_Click;

        _detailsPanel.Controls.Add(_previewBox);
        _detailsPanel.Controls.Add(_detailsLabel);
        _detailsPanel.Controls.Add(_btnUnpin);

        var bottomPanel = new Panel
        {
            Height = 40,                      
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(10, 4, 10, 4)
        };

        const int btnHeight = 30;
        
        _btnAdd = new Button
        {
            Text = "➕ Add",
            Size = new Size(110, btnHeight),
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
            Text = "📌 Pin / Open on Top",
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

        bottomPanel.Controls.Add(_btnAdd);
        bottomPanel.Controls.Add(_btnRemove);
        bottomPanel.Controls.Add(_btnPin);

        this.Text = "SkySticker - Image Library";
        this.Size = new Size(690, 510);
        this.MinimumSize = new Size(690, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(243, 243, 243);
        this.Resize += MainForm_Resize;

        this.Controls.Add(_searchBox);
        this.Controls.Add(_listView);
        this.Controls.Add(_detailsPanel);
        this.Controls.Add(bottomPanel);

        this.ResumeLayout(false);
        this.Load += (s, e) => MainForm_Resize(s, e);
        this.FormClosed += MainForm_FormClosed;
    }
    
    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        CloseAllOverlays();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        int listViewRight = _listView.Left + _listView.Width;
        int minDetailsPanelLeft = listViewRight + 16;
        int detailsPanelWidth = 250;
        int rightMargin = 12;
        int currentDetailsPanelLeft = this.ClientSize.Width - detailsPanelWidth - rightMargin;
        
        if (currentDetailsPanelLeft < minDetailsPanelLeft)
        {
            _detailsPanel.Left = minDetailsPanelLeft;
        }
        else
        {
            _detailsPanel.Left = currentDetailsPanelLeft;
        }
        _detailsPanel.Width = detailsPanelWidth;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _imageList.Images.Clear();
        foreach (var thumbnail in _thumbnailCache.Values)
        {
            thumbnail.Dispose();
        }
        _thumbnailCache.Clear();
        _previewBox.Image?.Dispose();
        base.OnFormClosing(e);
    }
}

