using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Serialization
{
    public static class FsfvJsonSerializer
    {

        public static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public static async Task SerializeToFile(string path, object value, CancellationToken cancellationToken = default)
        {
            await using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken);
        }

        public static async Task<T> DeserializeFromFile<T>(string path, CancellationToken cancellationToken = default)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
        }

    }
}
