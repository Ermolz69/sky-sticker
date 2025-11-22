namespace SkySticker.Forms;

public partial class OverlayForm
{
    // State management methods - all methods that mutate _imageItem and call _libraryService.Save
    
    protected void SaveState()
    {
        _libraryService.Save(_imageItems);
    }

    protected void ApplyFlipHorizontal(bool value)
    {
        _imageItem.FlipHorizontal = value;
        SaveState();
        this.Invalidate();
    }

    protected void ApplyFlipVertical(bool value)
    {
        _imageItem.FlipVertical = value;
        SaveState();
        this.Invalidate();
    }

    protected void ApplyRotation(float angle)
    {
        _imageItem.RotationAngle = angle;
        SaveState();
        this.Invalidate();
    }

    protected void ApplyRotationMode(bool enabled)
    {
        _imageItem.IsRotationModeEnabled = enabled;
        SaveState();
        this.Invalidate();
    }

    protected void ApplyAlwaysOnTop(bool value)
    {
        _imageItem.AlwaysOnTop = value;
        SaveState();
        UpdateTopMost();
    }

    protected void ApplyRename(string newName)
    {
        _imageItem.DisplayName = newName;
        SaveState();
        this.Text = _imageItem.DisplayName;
        this.Invalidate();
    }

    protected void SaveRotationState()
    {
        SaveState();
    }
}

