using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Custom health check response writer that provides both JSON and HTML responses.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    ///     Writes a custom health check response with HTML UI or JSON based on Accept header.
    /// </summary>
    public static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = context.Request.Headers.Accept.ToString().Contains("text/html")
            ? "text/html; charset=utf-8"
            : "application/json; charset=utf-8";

        if (context.Response.ContentType.Contains("text/html"))
            await WriteHtmlResponse(context, report);
        else
            await WriteJsonResponse(context, report);
    }

    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        var json = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        }, IndentedJsonOptions);

        await context.Response.WriteAsync(json);
    }

    private static async Task WriteHtmlResponse(HttpContext context, HealthReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset='utf-8' />");
        html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1' />");
        html.AppendLine("    <title>Freydis Health Check</title>");
        html.AppendLine("    <style>");
        html.AppendLine(
            "        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }");
        html.AppendLine("        .container { max-width: 1200px; margin: 0 auto; }");
        html.AppendLine("        h1 { color: #333; margin-bottom: 30px; }");
        html.AppendLine(
            "        .status-card { background: white; border-radius: 8px; padding: 20px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        html.AppendLine("        .status-healthy { border-left: 4px solid #4caf50; }");
        html.AppendLine("        .status-degraded { border-left: 4px solid #ff9800; }");
        html.AppendLine("        .status-unhealthy { border-left: 4px solid #f44336; }");
        html.AppendLine(
            "        .status-badge { display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 14px; font-weight: 500; }");
        html.AppendLine("        .badge-healthy { background: #e8f5e9; color: #2e7d32; }");
        html.AppendLine("        .badge-degraded { background: #fff3e0; color: #e65100; }");
        html.AppendLine("        .badge-unhealthy { background: #ffebee; color: #c62828; }");
        html.AppendLine("        .details { margin-top: 15px; }");
        html.AppendLine("        .detail-row { display: flex; padding: 8px 0; border-bottom: 1px solid #eee; }");
        html.AppendLine("        .detail-label { font-weight: 500; width: 200px; color: #666; }");
        html.AppendLine("        .detail-value { flex: 1; color: #333; }");
        html.AppendLine(
            "        .data-section { margin-top: 15px; background: #f5f5f5; padding: 15px; border-radius: 4px; }");
        html.AppendLine("        .data-section h4 { margin: 0 0 10px 0; color: #666; font-size: 14px; }");
        html.AppendLine(
            "        .collection-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px; }");
        html.AppendLine(
            "        .stat-box { background: white; padding: 12px; border-radius: 4px; text-align: center; }");
        html.AppendLine("        .stat-value { font-size: 24px; font-weight: 600; color: #333; }");
        html.AppendLine("        .stat-label { font-size: 12px; color: #666; margin-top: 4px; }");
        html.AppendLine(
            "        .header-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }");
        html.AppendLine("        .timestamp { color: #666; font-size: 14px; }");
        html.AppendLine(
            "        .refresh-btn { background: #2196f3; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-size: 14px; }");
        html.AppendLine("        .refresh-btn:hover { background: #1976d2; }");
        html.AppendLine(
            "        .error-message { background: #ffebee; color: #c62828; padding: 12px; border-radius: 4px; margin-top: 10px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class='container'>");
        html.AppendLine("        <h1>🔍 Freydis System Health</h1>");
        html.AppendLine("        <div class='header-info'>");
        html.AppendLine(CultureInfo.InvariantCulture,
            $"            <div class='timestamp'>Last checked: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>");
        html.AppendLine(
            "            <button class='refresh-btn' onclick='window.location.reload()'>🔄 Refresh</button>");
        html.AppendLine("        </div>");

        // Overall status
        var statusClass = report.Status switch
        {
            HealthStatus.Healthy => "healthy",
            HealthStatus.Degraded => "degraded",
            _ => "unhealthy"
        };

        html.AppendLine(CultureInfo.InvariantCulture,
            $"        <div class='status-card status-{statusClass}'>");
        html.AppendLine("            <h2>Overall Status</h2>");
        html.AppendLine(CultureInfo.InvariantCulture,
            $"            <span class='status-badge badge-{statusClass}'>{report.Status}</span>");
        html.AppendLine("            <div class='details'>");
        html.AppendLine("                <div class='detail-row'>");
        html.AppendLine("                    <div class='detail-label'>Total Duration</div>");
        html.AppendLine(CultureInfo.InvariantCulture,
            $"                    <div class='detail-value'>{report.TotalDuration.TotalMilliseconds:F2} ms</div>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Individual checks
        foreach (var entry in report.Entries)
        {
            var checkStatusClass = entry.Value.Status switch
            {
                HealthStatus.Healthy => "healthy",
                HealthStatus.Degraded => "degraded",
                _ => "unhealthy"
            };

            html.AppendLine(CultureInfo.InvariantCulture,
                $"        <div class='status-card status-{checkStatusClass}'>");
            html.AppendLine(CultureInfo.InvariantCulture,
                $"            <h3>{entry.Key}</h3>");
            html.AppendLine(CultureInfo.InvariantCulture,
                $"            <span class='status-badge badge-{checkStatusClass}'>{entry.Value.Status}</span>");

            if (!string.IsNullOrEmpty(entry.Value.Description))
                html.AppendLine(CultureInfo.InvariantCulture,
                    $"            <p>{entry.Value.Description}</p>");

            html.AppendLine("            <div class='details'>");
            html.AppendLine("                <div class='detail-row'>");
            html.AppendLine("                    <div class='detail-label'>Duration</div>");
            html.AppendLine(CultureInfo.InvariantCulture,
                $"                    <div class='detail-value'>{entry.Value.Duration.TotalMilliseconds:F2} ms</div>");
            html.AppendLine("                </div>");

            // Display data if available
            if (entry.Value.Data?.Count > 0)
                foreach (var data in entry.Value.Data)
                    if (data.Key == "Collections")
                    {
                        html.AppendLine("                <div class='data-section'>");
                        html.AppendLine("                    <h4>📊 Collection Statistics</h4>");
                        html.AppendLine("                    <div class='collection-stats'>");

                        if (TryGetObjectProperties(data.Value, out var collections))
                            foreach (var collection in collections)
                            {
                                html.AppendLine("                        <div class='stat-box'>");
                                html.AppendLine(CultureInfo.InvariantCulture,
                                    $"                            <div class='stat-value'>{collection.Value}</div>");
                                html.AppendLine(CultureInfo.InvariantCulture,
                                    $"                            <div class='stat-label'>{collection.Key}</div>");
                                html.AppendLine("                        </div>");
                            }

                        html.AppendLine("                    </div>");
                        html.AppendLine("                </div>");
                    }
                    else if (data.Key == "MemoryUsage")
                    {
                        html.AppendLine("                <div class='data-section'>");
                        html.AppendLine("                    <h4>💾 Memory Usage</h4>");
                        html.AppendLine("                    <div class='collection-stats'>");

                        if (TryGetObjectProperties(data.Value, out var memory))
                            foreach (var item in memory)
                            {
                                html.AppendLine("                        <div class='stat-box'>");
                                html.AppendLine(CultureInfo.InvariantCulture,
                                    $"                            <div class='stat-value'>{item.Value}</div>");
                                html.AppendLine(CultureInfo.InvariantCulture,
                                    $"                            <div class='stat-label'>{item.Key}</div>");
                                html.AppendLine("                        </div>");
                            }

                        html.AppendLine("                    </div>");
                        html.AppendLine("                </div>");
                    }
                    else if (data.Key == "AgentDetails")
                    {
                        html.AppendLine("                <div class='data-section'>");
                        html.AppendLine("                    <h4>🤖 Agent Details</h4>");

                        html.AppendLine("                    <div style='overflow-x: auto;'>");
                        html.AppendLine(
                            "                        <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>");
                        html.AppendLine("                            <thead>");
                        html.AppendLine("                                <tr style='background: #f0f0f0;'>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Agent</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: center;'>Status</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: center;'>Available</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: center;'>Active</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: center;'>Completed</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: center;'>Success Rate</th>");
                        html.AppendLine(
                            "                                    <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Status Message</th>");
                        html.AppendLine("                                </tr>");
                        html.AppendLine("                            </thead>");
                        html.AppendLine("                            <tbody>");

                        // Dynamically render actual agent data
                        if (data.Value is IEnumerable<object> agentDetails)
                        {
                            foreach (var agent in agentDetails)
                                if (agent != null)
                                {
                                    var agentType = agent.GetType();
                                    var name = agentType.GetProperty("Name")?.GetValue(agent)?.ToString() ?? "Unknown";
                                    var isHealthy = (bool?)agentType.GetProperty("IsHealthy")?.GetValue(agent) ?? false;
                                    var isAvailable = (bool?)agentType.GetProperty("IsAvailable")?.GetValue(agent) ??
                                                      false;
                                    var activeExecutions = agentType.GetProperty("ActiveExecutions")?.GetValue(agent)
                                        ?.ToString() ?? "0";
                                    var totalExecutions = agentType.GetProperty("TotalExecutions")?.GetValue(agent)
                                        ?.ToString() ?? "0";
                                    var successRate =
                                        agentType.GetProperty("SuccessRate")?.GetValue(agent)?.ToString() ?? "100%";
                                    var status = agentType.GetProperty("Status")?.GetValue(agent)?.ToString() ??
                                                 "Unknown";

                                    var healthIcon = isHealthy ? "✅ Healthy" : "❌ Unhealthy";
                                    var availableIcon = isAvailable ? "✅ Available" : "❌ Unavailable";

                                    html.AppendLine("                                <tr>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd;'>{name}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{healthIcon}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{availableIcon}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{activeExecutions}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{totalExecutions}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{successRate}</td>");
                                    html.AppendLine(CultureInfo.InvariantCulture,
                                        $"                                    <td style='padding: 8px; border: 1px solid #ddd;'>{status}</td>");
                                    html.AppendLine("                                </tr>");
                                }
                        }
                        else
                        {
                            // Fallback if agent data is not available
                            html.AppendLine("                                <tr>");
                            html.AppendLine(
                                "                                    <td colspan='7' style='padding: 16px; border: 1px solid #ddd; text-align: center; color: #666;'>No agent data available</td>");
                            html.AppendLine("                                </tr>");
                        }

                        html.AppendLine("                            </tbody>");
                        html.AppendLine("                        </table>");
                        html.AppendLine("                    </div>");
                        html.AppendLine("                </div>");
                    }
                    else
                    {
                        html.AppendLine("                <div class='detail-row'>");
                        html.AppendLine(CultureInfo.InvariantCulture,
                            $"                    <div class='detail-label'>{data.Key}</div>");
                        html.AppendLine(CultureInfo.InvariantCulture,
                            $"                    <div class='detail-value'>{data.Value}</div>");
                        html.AppendLine("                </div>");
                    }

            // Display exception if any
            if (entry.Value.Exception != null)
            {
                html.AppendLine("                <div class='error-message'>");
                html.AppendLine(CultureInfo.InvariantCulture,
                    $"                    <strong>Error:</strong> {entry.Value.Exception.Message}");
                html.AppendLine("                </div>");
            }

            html.AppendLine("            </div>");
            html.AppendLine("        </div>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <script>");
        html.AppendLine("        // Auto-refresh every 30 seconds");
        html.AppendLine("        setTimeout(() => window.location.reload(), 30000);");
        html.AppendLine("    </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        await context.Response.WriteAsync(html.ToString());
    }

    private static bool TryGetObjectProperties(object? obj, out Dictionary<string, object> properties)
    {
        properties = new Dictionary<string, object>();

        if (obj == null)
            return false;

        try
        {
            // First try to serialize and deserialize to handle anonymous objects
            var json = JsonSerializer.Serialize(obj);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonElement.EnumerateObject())
                {
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => "null",
                        _ => (object)property.Value.ToString()
                    };
                    properties[property.Name] = value ?? "null";
                }

                return true;
            }
        }
        catch
        {
            // Fallback to reflection if JSON serialization fails
            try
            {
                var type = obj.GetType();
                var propertyInfos = type.GetProperties();

                foreach (var prop in propertyInfos)
                {
                    var value = prop.GetValue(obj);
                    properties[prop.Name] = value ?? "null";
                }

                return properties.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}