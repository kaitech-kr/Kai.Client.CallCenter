using System.IO;
using System.Text.Json;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
public class JsonFileManager
{
    private static readonly string s_ConfigPath = "appsettings.json";

    // 읽기
    public static string GetValue(string key)
    {
        if (!File.Exists(s_ConfigPath))
            return string.Empty;

        string json = File.ReadAllText(s_ConfigPath);
        using JsonDocument doc = JsonDocument.Parse(json);

        var keys = key.Split(':');
        JsonElement current = doc.RootElement;

        foreach (var k in keys)
        {
            if (!current.TryGetProperty(k, out current))
                return string.Empty;
        }

        return current.ToString();
    }

    // 쓰기
    public static void SetValue(string key, string value)
    {
        string json = File.Exists(s_ConfigPath) ? File.ReadAllText(s_ConfigPath) : "{}";

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        dict ??= new Dictionary<string, object>();

        var keys = key.Split(':');
        // 간단하게 1레벨만 지원
        dict[keys[0]] = value;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(s_ConfigPath, JsonSerializer.Serialize(dict, options));
    }
}

#nullable enable
