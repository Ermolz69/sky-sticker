using SkySticker.Helpers;

namespace SkySticker.Forms;

public partial class OverlayForm
{
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
        SaveState();
        
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
        // Always apply the change, even if the state appears to be the same
        // This ensures synchronization when called from MainForm
        _imageItem.IsPinned = pinned;
        SaveState();
        
        // Set click-through mode (clicks pass through the window when pinned)
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
        UpdateTopMost();
        
        // If pinned, hide interactive elements
        if (_imageItem.IsPinned)
        {
            _isHovered = false;
            if (_settingsButton != null)
                _settingsButton.Visible = false;
        }
        else
        {
            // If unpinned, allow interactive elements to show on hover
            // The hover state will be managed by MouseEnter/MouseLeave events
        }
        this.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;

        if (_imageItem.IsPinned && m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }
}

