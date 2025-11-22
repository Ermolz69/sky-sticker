using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SkySticker.Forms;

public partial class OverlayForm
{
    // Cache opacity value to detect changes
    private double _cachedOpacity = -1;
    
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));

        if (_originalImage == null) return;

        var g = e.Graphics;
        
        // Use faster settings during resize for smoothness, high quality when idle
        if (_isResizing)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.CompositingMode = CompositingMode.SourceOver;
        }
        else
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.CompositingMode = CompositingMode.SourceOver;
        }

        var clientRect = this.ClientRectangle;
        var imageRect = CalculateImageRect(clientRect, _originalImage.Size);
        var opacity = _fadeOpacity * (_imageItem.Opacity / 100.0);
        
        if (_cachedImageAttributes == null || Math.Abs(_cachedOpacity - opacity) > 0.01)
        {
            _cachedImageAttributes?.Dispose();
            _cachedImageAttributes = new ImageAttributes();
            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, (float)opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            _cachedImageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            _cachedImageAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
            _cachedOpacity = opacity;
        }
        
        var imageAttributes = _cachedImageAttributes;
        var transform = g.Transform;
        var centerX = imageRect.X + imageRect.Width / 2.0f;
        var centerY = imageRect.Y + imageRect.Height / 2.0f;
        
        if (_imageItem.RotationAngle != 0 || _imageItem.FlipHorizontal || _imageItem.FlipVertical)
        {
            g.TranslateTransform(centerX, centerY);
            
            if (_imageItem.RotationAngle != 0)
            {
                g.RotateTransform(_imageItem.RotationAngle);
            }
            
            if (_imageItem.FlipHorizontal || _imageItem.FlipVertical)
            {
                float scaleX = _imageItem.FlipHorizontal ? -1.0f : 1.0f;
                float scaleY = _imageItem.FlipVertical ? -1.0f : 1.0f;
                g.ScaleTransform(scaleX, scaleY);
            }
            
            g.TranslateTransform(-centerX, -centerY);
        }

        g.DrawImage(
            _originalImage, 
            imageRect, 
            0, 0, 
            _originalImage.Width, 
            _originalImage.Height, 
            GraphicsUnit.Pixel, 
            imageAttributes);
        
        g.Transform = transform;

        if (_isHovered)
        {
            var borderColor = _imageItem.IsRotationModeEnabled 
                ? Color.FromArgb(100, 255, 200, 0)
                : Color.FromArgb(68, 255, 255, 255);
            using var borderPen = new Pen(borderColor, BorderThickness);
            g.DrawRectangle(borderPen, clientRect.X + BorderThickness / 2, clientRect.Y + BorderThickness / 2,
                clientRect.Width - BorderThickness, clientRect.Height - BorderThickness);
        }
        
        if (_imageItem.IsRotationModeEnabled && _isHovered)
        {
            using var font = new Font("Arial", 9, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(200, 255, 200, 0));
            var text = "Rotation Mode (R)";
            var textSize = g.MeasureString(text, font);
            g.DrawString(text, font, textBrush, 
                clientRect.Left + 5, 
                clientRect.Top + 5);
        }

        if (_isHovered && !_imageItem.IsRotationModeEnabled)
        {
            DrawResizeHandles(g, clientRect);
        }
    }

    protected Rectangle CalculateImageRect(Rectangle clientRect, Size imageSize)
    {
        if (clientRect.Width <= 0 || clientRect.Height <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

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

    protected void DrawResizeHandles(Graphics g, Rectangle rect)
    {
        using var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        var size = ResizeHandleSize;

        g.FillRectangle(brush, rect.Left, rect.Top, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Left, rect.Top + rect.Height / 2 - size / 2, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top + rect.Height / 2 - size / 2, size, size);
    }
}

