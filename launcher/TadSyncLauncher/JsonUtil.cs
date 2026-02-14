using System.IO;
using System.Text.Json;

namespace TadSyncLauncher
{
  public static class JsonUtil
  {
    private static readonly JsonSerializerOptions Opt = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    public static T? Read<T>(string path) where T : class
    {
      if (!File.Exists(path)) return null;
      var json = File.ReadAllText(path);
      return JsonSerializer.Deserialize<T>(json, Opt);
    }

    public static void Write<T>(string path, T obj) where T : class
    {
      var dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

      var json = JsonSerializer.Serialize(obj, Opt);
      File.WriteAllText(path, json);
    }
  }
}
