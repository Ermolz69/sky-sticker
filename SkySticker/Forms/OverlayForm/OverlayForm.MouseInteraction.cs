using SkySticker.Helpers;

namespace SkySticker.Forms;

public partial class OverlayForm
{
    private Rectangle[]? _resizeHandleRects;
    private Rectangle _lastClientRect;
    
    private void OverlayForm_MouseEnter(object? sender, EventArgs e)
    {
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
        if (_imageItem.IsPinned)
            return;
            
        if (e.Button == MouseButtons.Left)
        {
            if (_imageItem.IsRotationModeEnabled)
            {
                _isRotating = true;
                _rotationStartPoint = e.Location;
                _rotationStartAngle = _imageItem.RotationAngle;
                this.Cursor = Cursors.Cross;
            }
            else
            {
                _activeResizeHandle = GetResizeHandle(e.Location);
                if (_activeResizeHandle == ResizeHandle.None)
                {
                    _isDragging = true;
                    _dragStartPoint = e.Location;
                    _formStartLocation = this.Location;
                    WinApiHelper.MoveWindow(this.Handle);
                }
                else
                {
                    _dragStartPoint = e.Location;
                    _isResizing = true;
                }
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            _contextMenu?.Show(this, e.Location);
        }
    }

    private void OverlayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_imageItem.IsPinned)
        {
            this.Cursor = Cursors.Default;
            return;
        }
        
        if (_isRotating && e.Button == MouseButtons.Left)
        {
            var centerX = this.ClientRectangle.Width / 2.0f;
            var centerY = this.ClientRectangle.Height / 2.0f;
            
            var startAngle = (float)(Math.Atan2(_rotationStartPoint.Y - centerY, _rotationStartPoint.X - centerX) * 180.0 / Math.PI);
            var currentAngle = (float)(Math.Atan2(e.Location.Y - centerY, e.Location.X - centerX) * 180.0 / Math.PI);
            var deltaAngle = currentAngle - startAngle;
            
            _imageItem.RotationAngle = (_rotationStartAngle + deltaAngle) % 360;
            if (_imageItem.RotationAngle < 0)
                _imageItem.RotationAngle += 360;
            
            ThrottledInvalidate();
        }
        else if (_isDragging && e.Button == MouseButtons.Left)
        {
            // Dragging is handled via WinAPI
        }
        else if (_activeResizeHandle != ResizeHandle.None && e.Button == MouseButtons.Left)
        {
            ResizeWindow(e.Location);
        }
        else
        {
            if (_imageItem.IsRotationModeEnabled)
            {
                this.Cursor = Cursors.Cross;
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
    }

    private void OverlayForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_isRotating)
            {
                _isRotating = false;
                SaveRotationState();
            }
            
            if (_isResizing || _activeResizeHandle != ResizeHandle.None)
            {
                _isResizing = false;
                SavePositionAndSize();
                this.Invalidate();
            }
            
            if (_isDragging)
            {
                SavePositionAndSize();
            }
            
            _isDragging = false;
            _activeResizeHandle = ResizeHandle.None;
            this.Cursor = Cursors.Default;
        }
    }

    private void OverlayForm_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_imageItem.IsPinned)
            return;
            
        if (e.Button == MouseButtons.Right)
        {
            _contextMenu?.Show(this, e.Location);
        }
    }

    private void UpdateResizeHandles()
    {
        var rect = this.ClientRectangle;
        var size = ResizeHandleSize;
        
        _resizeHandleRects = new[]
        {
            new Rectangle(rect.Left, rect.Top, size, size), // TopLeft
            new Rectangle(rect.Right - size, rect.Top, size, size), // TopRight
            new Rectangle(rect.Left, rect.Bottom - size, size, size), // BottomLeft
            new Rectangle(rect.Right - size, rect.Bottom - size, size, size), // BottomRight
            new Rectangle(rect.Left + rect.Width / 2 - size / 2, rect.Top, size, size), // Top
            new Rectangle(rect.Left + rect.Width / 2 - size / 2, rect.Bottom - size, size, size), // Bottom
            new Rectangle(rect.Left, rect.Top + rect.Height / 2 - size / 2, size, size), // Left
            new Rectangle(rect.Right - size, rect.Top + rect.Height / 2 - size / 2, size, size) // Right
        };
        _lastClientRect = rect;
    }

    protected ResizeHandle GetResizeHandle(Point mousePos)
    {
        var rect = this.ClientRectangle;
        
        if (_resizeHandleRects == null || !rect.Equals(_lastClientRect))
        {
            UpdateResizeHandles();
        }

        if (_resizeHandleRects == null) return ResizeHandle.None;

        if (_resizeHandleRects[0].Contains(mousePos)) return ResizeHandle.TopLeft;
        if (_resizeHandleRects[1].Contains(mousePos)) return ResizeHandle.TopRight;
        if (_resizeHandleRects[2].Contains(mousePos)) return ResizeHandle.BottomLeft;
        if (_resizeHandleRects[3].Contains(mousePos)) return ResizeHandle.BottomRight;
        if (_resizeHandleRects[4].Contains(mousePos)) return ResizeHandle.Top;
        if (_resizeHandleRects[5].Contains(mousePos)) return ResizeHandle.Bottom;
        if (_resizeHandleRects[6].Contains(mousePos)) return ResizeHandle.Left;
        if (_resizeHandleRects[7].Contains(mousePos)) return ResizeHandle.Right;

        return ResizeHandle.None;
    }

    protected Cursor GetCursorForHandle(ResizeHandle handle)
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

    protected void ResizeWindow(Point mousePos)
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
            this.SetBounds(newLocation.X, newLocation.Y, newSize.Width, newSize.Height, BoundsSpecified.All);
            _dragStartPoint = mousePos;
            
            // Use Refresh() for synchronous repaint during resize to prevent visual lag
            this.Refresh();
        }
    }
}

