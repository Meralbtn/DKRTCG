
using Newtonsoft.Json;

public class CardConfigData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Cost { get; set; }
    public string EffectType;
    public int EffectValue;    
    public string Desc { get; set; }
}
//读取卡牌数据
public class CardConfigManager
{
    public static CardConfigManager Instance { get; } = new CardConfigManager();

    private Dictionary<int, CardConfigData> _configs = new();
    private FileSystemWatcher _watcher;
    private string _filePath;

    private CardConfigManager() { }

    public void Load(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
        Console.WriteLine($"[CardConfig] 加载路径: {_filePath}");
        LoadFile();
        WatchFile();
    }
    private void LoadFile()
    {
        try
        {
            string json = File.ReadAllText(_filePath);
            var list = JsonConvert.DeserializeObject<List<CardConfigData>>(json);
            var newDict = list.ToDictionary(c => c.Id);

            _configs = newDict;
            Console.WriteLine($"[CardConfig] 加载成功，共 {_configs.Count} 张卡牌");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CardConfig] 加载失败: {ex.Message}");
        }
    }

    private void WatchFile()
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(_filePath))!;
        string file = Path.GetFileName(_filePath);

        _watcher = new FileSystemWatcher(dir, file);

        _watcher.Changed += (_, _) =>
        {
            Thread.Sleep(100);
            LoadFile();
            Console.WriteLine("[CardConfig] 检测到文件变化，已热重载");
        };

        _watcher.EnableRaisingEvents = true;
    }

    public bool TryGet(int id, out CardConfigData config)
        => _configs.TryGetValue(id, out config);
}