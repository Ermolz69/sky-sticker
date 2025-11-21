namespace SkySticker.Models;

public class ImageItem
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string FilePath { get; set; } = "";
    // Последние параметры окна-стикера
    public int? LastX { get; set; }
    public int? LastY { get; set; }
    public int? LastWidth { get; set; }
    public int? LastHeight { get; set; }
    // Дополнительные параметры
    public int Opacity { get; set; } = 100; // 0-100
    public bool AlwaysOnTop { get; set; } = true;
    public DateTime? LastUsed { get; set; }
    public int CornerRadius { get; set; } = 0; // Радиус скругления углов
    public bool IsPinned { get; set; } = false; // Закреплено ли изображение (отключает интеракцию)
}

