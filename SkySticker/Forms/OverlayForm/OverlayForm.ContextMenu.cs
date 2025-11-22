using SkySticker.Dialogs;
using SkySticker.Helpers;

namespace SkySticker.Forms;

public partial class OverlayForm
{
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
            ApplyAlwaysOnTop(!_imageItem.AlwaysOnTop);
            alwaysOnTopItem.Checked = _imageItem.AlwaysOnTop;
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

        // Flip menu
        var flipMenu = new ToolStripMenuItem("Flip");
        
        // Flip Horizontal
        var flipHorizontalItem = new ToolStripMenuItem("Flip Horizontal");
        flipHorizontalItem.Checked = _imageItem.FlipHorizontal;
        flipHorizontalItem.Click += (s, e) =>
        {
            ApplyFlipHorizontal(!_imageItem.FlipHorizontal);
            flipHorizontalItem.Checked = _imageItem.FlipHorizontal;
        };
        flipMenu.DropDownItems.Add(flipHorizontalItem);
        
        // Flip Vertical
        var flipVerticalItem = new ToolStripMenuItem("Flip Vertical");
        flipVerticalItem.Checked = _imageItem.FlipVertical;
        flipVerticalItem.Click += (s, e) =>
        {
            ApplyFlipVertical(!_imageItem.FlipVertical);
            flipVerticalItem.Checked = _imageItem.FlipVertical;
        };
        flipMenu.DropDownItems.Add(flipVerticalItem);
        
        _contextMenu.Items.Add(flipMenu);

        // Enable/Disable Rotation Mode
        var rotationModeItem = new ToolStripMenuItem(_imageItem.IsRotationModeEnabled ? "Disable Rotation Mode (R)" : "Enable Rotation Mode (R)");
        rotationModeItem.Checked = _imageItem.IsRotationModeEnabled;
        rotationModeItem.Click += (s, e) =>
        {
            ApplyRotationMode(!_imageItem.IsRotationModeEnabled);
            rotationModeItem.Checked = _imageItem.IsRotationModeEnabled;
            rotationModeItem.Text = _imageItem.IsRotationModeEnabled ? "Disable Rotation Mode (R)" : "Enable Rotation Mode (R)";
        };
        _contextMenu.Items.Add(rotationModeItem);

        // Reset Rotation
        var resetRotationItem = new ToolStripMenuItem("Reset Rotation");
        resetRotationItem.Click += (s, e) =>
        {
            ApplyRotation(0);
        };
        _contextMenu.Items.Add(resetRotationItem);

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
            if (MessageBox.Show($"Remove '{_imageItem.DisplayName}' from library?", "Remove",
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
                ApplyRename(dialog.NewName);
            }
        };
        _contextMenu.Items.Add(renameItem);
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
        SaveState();
    }

    private void OverlayForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.R && !_imageItem.IsPinned)
        {
            // Toggle rotation mode with R key
            ApplyRotationMode(!_imageItem.IsRotationModeEnabled);
            
            // Update context menu item if menu exists
            if (_contextMenu != null)
            {
                foreach (ToolStripItem item in _contextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.Text != null &&
                        (menuItem.Text.Contains("Enable Rotation Mode") || menuItem.Text.Contains("Disable Rotation Mode")))
                    {
                        menuItem.Checked = _imageItem.IsRotationModeEnabled;
                        menuItem.Text = _imageItem.IsRotationModeEnabled ? "Disable Rotation Mode (R)" : "Enable Rotation Mode (R)";
                        break;
                    }
                }
            }
            
            this.Invalidate();
            e.Handled = true;
        }
    }
}

