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
        if (_imageItem.IsPinned && this.TopMost && !_temporarilyLowered)
        {
            this.TopMost = false;
            _temporarilyLowered = true;
        }
    }
    
    public void RestoreTopMost()
    {
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
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
        UpdateTopMost();
        
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
        _imageItem.IsPinned = pinned;
        SaveState();
        WinApiHelper.SetClickThrough(this.Handle, _imageItem.IsPinned);
        UpdateTopMost();
        
        if (_imageItem.IsPinned)
        {
            _isHovered = false;
            if (_settingsButton != null)
                _settingsButton.Visible = false;
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

