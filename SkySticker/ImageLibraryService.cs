using System.Text.Json;

namespace SkySticker;

public class ImageLibraryService
{
    private readonly string _libraryPath;

    public ImageLibraryService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "SkySticker");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        
        _libraryPath = Path.Combine(appFolder, "library.json");
    }

    public List<ImageItem> Load()
    {
        if (!File.Exists(_libraryPath))
        {
            return new List<ImageItem>();
        }

        try
        {
            var json = File.ReadAllText(_libraryPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ImageItem>();
            }

            var items = JsonSerializer.Deserialize<List<ImageItem>>(json);
            return items ?? new List<ImageItem>();
        }
        catch (Exception)
        {
            return new List<ImageItem>();
        }
    }

    public void Save(List<ImageItem> items)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(items, options);
            File.WriteAllText(_libraryPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении библиотеки: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

