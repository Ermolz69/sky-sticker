using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SkySticker.Forms;

public partial class OverlayForm
{
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Рисуем фон редким цветом RGB(1,0,1) - он будет прозрачным через COLORKEY
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Очищаем область редким цветом RGB(1,0,1) - он станет прозрачным через COLORKEY
        e.Graphics.Clear(Color.FromArgb(1, 0, 1));

        if (_originalImage == null) return;

        var g = e.Graphics;
        // Используем высокое качество для избежания артефактов на краях
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        // Важно: Half для правильной обработки пикселей без смещения
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.CompositingQuality = CompositingQuality.HighQuality;
        // Используем SourceOver для правильной обработки прозрачности PNG
        g.CompositingMode = CompositingMode.SourceOver;

        var clientRect = this.ClientRectangle;

        // Рисуем изображение с сохранением пропорций
        var imageRect = CalculateImageRect(clientRect, _originalImage.Size);
        
        // Применяем fade-in эффект
        var opacity = _fadeOpacity * (_imageItem.Opacity / 100.0);
        var imageAttributes = new ImageAttributes();
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, (float)opacity, 0},
            new float[] {0, 0, 0, 0, 1}
        });
        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        // Важно: отключаем обрезку для правильной обработки краев
        imageAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);

        // Apply transformations for flipping and rotation
        var transform = g.Transform;
        var centerX = imageRect.X + imageRect.Width / 2.0f;
        var centerY = imageRect.Y + imageRect.Height / 2.0f;
        
        // Apply transformations in order: translate to center, rotate, flip, translate back
        if (_imageItem.RotationAngle != 0 || _imageItem.FlipHorizontal || _imageItem.FlipVertical)
        {
            // Move to center
            g.TranslateTransform(centerX, centerY);
            
            // Apply rotation
            if (_imageItem.RotationAngle != 0)
            {
                g.RotateTransform(_imageItem.RotationAngle);
            }
            
            // Apply flipping
            if (_imageItem.FlipHorizontal || _imageItem.FlipVertical)
            {
                float scaleX = _imageItem.FlipHorizontal ? -1.0f : 1.0f;
                float scaleY = _imageItem.FlipVertical ? -1.0f : 1.0f;
                g.ScaleTransform(scaleX, scaleY);
            }
            
            // Move back
            g.TranslateTransform(-centerX, -centerY);
        }

        // Рисуем изображение с правильной обработкой прозрачности PNG
        // Фон уже очищен прозрачным цветом RGB(1,0,1), который станет прозрачным через COLORKEY
        g.DrawImage(
            _originalImage, 
            imageRect, 
            0, 0, 
            _originalImage.Width, 
            _originalImage.Height, 
            GraphicsUnit.Pixel, 
            imageAttributes);
        
        // Восстанавливаем трансформацию
        g.Transform = transform;

        // Рисуем рамку при наведении
        if (_isHovered)
        {
            // Use different color when rotation mode is enabled
            var borderColor = _imageItem.IsRotationModeEnabled 
                ? Color.FromArgb(100, 255, 200, 0) // Orange/yellow when rotation mode is active
                : Color.FromArgb(68, 255, 255, 255); // White normally
            using var borderPen = new Pen(borderColor, BorderThickness);
            g.DrawRectangle(borderPen, clientRect.X + BorderThickness / 2, clientRect.Y + BorderThickness / 2,
                clientRect.Width - BorderThickness, clientRect.Height - BorderThickness);
        }
        
        // Show rotation mode indicator
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

        // Рисуем resize handles при наведении (только если режим поворота выключен)
        if (_isHovered && !_imageItem.IsRotationModeEnabled)
        {
            DrawResizeHandles(g, clientRect);
        }

        imageAttributes.Dispose();
    }

    protected Rectangle CalculateImageRect(Rectangle clientRect, Size imageSize)
    {
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

        // Углы
        g.FillRectangle(brush, rect.Left, rect.Top, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Bottom - size, size, size);

        // Стороны
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Top, size, size);
        g.FillRectangle(brush, rect.Left + rect.Width / 2 - size / 2, rect.Bottom - size, size, size);
        g.FillRectangle(brush, rect.Left, rect.Top + rect.Height / 2 - size / 2, size, size);
        g.FillRectangle(brush, rect.Right - size, rect.Top + rect.Height / 2 - size / 2, size, size);
    }
}

