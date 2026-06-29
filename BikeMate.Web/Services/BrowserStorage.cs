using System.Text.Json;
using Microsoft.JSInterop;

namespace BikeMate.Web.Services;

public sealed class BrowserStorage(IJSRuntime js)
{
    public ValueTask SetItemAsync(string key, string value)
        => js.InvokeVoidAsync("sessionStorage.setItem", key, value);

    public ValueTask SetItemAsync<T>(string key, T value)
        => js.InvokeVoidAsync("sessionStorage.setItem", key, JsonSerializer.Serialize(value));

    public async ValueTask<string?> GetItemAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("sessionStorage.getItem", key);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var value = await GetItemAsync(key);
        return string.IsNullOrWhiteSpace(value) ? default : JsonSerializer.Deserialize<T>(value);
    }

    public ValueTask RemoveAsync(string key)
        => js.InvokeVoidAsync("sessionStorage.removeItem", key);
}
