using SkySticker.Helpers;

namespace SkySticker.Forms;

public partial class OverlayForm
{
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
            if (_imageItem.IsRotationModeEnabled)
            {
                // Start rotation when rotation mode is enabled
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
        
        if (_isRotating && e.Button == MouseButtons.Left)
        {
            // Calculate rotation angle based on mouse movement
            var centerX = this.ClientRectangle.Width / 2.0f;
            var centerY = this.ClientRectangle.Height / 2.0f;
            
            // Calculate angle from center to start point
            var startAngle = (float)(Math.Atan2(_rotationStartPoint.Y - centerY, _rotationStartPoint.X - centerX) * 180.0 / Math.PI);
            
            // Calculate angle from center to current point
            var currentAngle = (float)(Math.Atan2(e.Location.Y - centerY, e.Location.X - centerX) * 180.0 / Math.PI);
            
            // Calculate rotation delta
            var deltaAngle = currentAngle - startAngle;
            
            // Apply rotation
            _imageItem.RotationAngle = (_rotationStartAngle + deltaAngle) % 360;
            if (_imageItem.RotationAngle < 0)
                _imageItem.RotationAngle += 360;
            
            this.Invalidate();
        }
        else if (_isDragging && e.Button == MouseButtons.Left)
        {
            // Перемещение обрабатывается через WinAPI
        }
        else if (_activeResizeHandle != ResizeHandle.None && e.Button == MouseButtons.Left)
        {
            ResizeWindow(e.Location);
        }
        else
        {
            if (_imageItem.IsRotationModeEnabled)
            {
                // Show rotation cursor when rotation mode is enabled
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
                // Rotation was performed, save it
                _isRotating = false;
                SaveRotationState();
            }
            _isDragging = false;
            _activeResizeHandle = ResizeHandle.None;
            this.Cursor = Cursors.Default;
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

    protected ResizeHandle GetResizeHandle(Point mousePos)
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
            this.Location = newLocation;
            this.Size = newSize;
            _dragStartPoint = mousePos;
        }
    }
}

