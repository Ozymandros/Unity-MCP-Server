using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace UnityMcp.Application.Tools;

/// <summary>
/// Link-only Unity docs helper. Returns official Unity docs search URLs and canonical doc links
/// for a given query without fetching or caching any content. This implementation
/// is intentionally conservative to remain compliant with Unity's Terms of Service.
/// </summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_search_docs"), Description("Return official Unity docs search and canonical links for a query. No content is fetched or cached.")]
    public static Task<string> SearchUnityDocs(
        [Description("Query string to search for")] string query,
        [Description("Maximum results to return") ] int maxResults = 5,
        [Description("(Ignored) Seed cache - kept for backward compatibility; no caching performed")] bool seed = false,
        [Description("(Ignored) Cache directory - kept for backward compatibility; no caching performed")] string cacheDir = "DocsCache",
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        var results = new List<object>();

        // Primary: official Manual search URL (works for general/manual content)
        var encoded = Uri.EscapeDataString(q);
        var manualSearch = string.IsNullOrEmpty(encoded)
            ? "https://docs.unity3d.com/Manual/index.html"
            : $"https://docs.unity3d.com/Manual/30_search.html?q={encoded}";
        results.Add(new { title = "Unity Manual — search", url = manualSearch, note = "Opens Unity Manual search; no content fetched." });

        // Secondary: Script Reference (useful when user is likely asking about API)
        if (results.Count < maxResults)
            results.Add(new { title = "Unity Scripting Reference", url = "https://docs.unity3d.com/ScriptReference/", note = "Official API reference (browse or search)." });

        // Heuristics: add a context-specific direct page when query contains strong keywords
        var qc = q.ToLowerInvariant();
        if (qc.Contains("animation") && results.Count < maxResults)
        {
            results.Add(new { title = "Animation (Manual)", url = "https://docs.unity3d.com/Manual/AnimationOverview.html", note = "Direct Manual page for Animation." });
        }
        else if ((qc.Contains("urp") || qc.Contains("universal render")) && results.Count < maxResults)
        {
            results.Add(new { title = "Universal Render Pipeline (URP)", url = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest", note = "Package docs for URP." });
        }

        // Fill with general Manual index if still under maxResults
        while (results.Count < Math.Max(1, Math.Min(maxResults, 3)))
        {
            results.Add(new { title = "Unity Manual", url = "https://docs.unity3d.com/Manual/index.html", note = "Official Unity Manual." });
        }

        var json = JsonSerializer.Serialize(results.ToArray(), new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(json);
    }
}