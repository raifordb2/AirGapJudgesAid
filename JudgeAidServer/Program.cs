using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using JudgeAidServer.Models;
using JudgesAid.Models;

var builder = WebApplication.CreateBuilder(args);

// Listen on every IPv4 interface so other laptops can connect.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5000);
});

var app = builder.Build();

app.MapGet("/", () => "JudgeAid Server");

app.MapGet("/api/status", () =>
{
    var dataPath = builder.Configuration["DataPath"];

    if (string.IsNullOrWhiteSpace(dataPath))
    {
        return Results.Problem("DataPath is not configured.", statusCode: StatusCodes.Status500InternalServerError);
    }

    var fullDataPath = Path.GetFullPath(dataPath);
    Directory.CreateDirectory(fullDataPath);

    var eventListCandidates = new[] { "EventsList.json", "EventList.json" };
    string? eventListPath = null;

    foreach (var candidate in eventListCandidates)
    {
        var path = Path.Combine(fullDataPath, candidate);
        if (File.Exists(path))
        {
            eventListPath = path;
            break;
        }
    }

    if (eventListPath is null)
    {
        return Results.NotFound(new
        {
            Status = "Connected",
            ServerName = Environment.MachineName,
            ServerTime = DateTime.Now,
            DataPath = fullDataPath,
            IsInitialized = false,
            MissingFiles = new[] { "EventsList.json" }
        });
    }

    try
    {
        var json = File.ReadAllText(eventListPath);
        var eventList = JsonSerializer.Deserialize<EventListModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (eventList is null)
        {
            return Results.Problem("Unable to deserialize the event list file.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var missingFiles = new List<string>();

        foreach (var evt in eventList.EventList)
        {
            if (string.IsNullOrWhiteSpace(evt.EventId))
            {
                continue;
            }

            var notebookFiles = Directory.EnumerateFiles(fullDataPath, $"DigitalEngineeringNotebooks-{evt.EventId}*.csv", SearchOption.TopDirectoryOnly).ToList();

            foreach (var notebookFile in notebookFiles)
            {
                ConvertWorkbookToJson(notebookFile);
            }

            if (notebookFiles.Count == 0)
            {
                missingFiles.Add($"DigitalEngineeringNotebooks-{evt.EventId}*.xlsx");
            }

        }

        var initialized = missingFiles.Count == 0;

        return Results.Ok(new
        {
            Status = "Connected",
            ServerName = Environment.MachineName,
            ServerTime = DateTime.Now,
            DataPath = fullDataPath,
            EventCount = eventList.EventList.Count,
            IsInitialized = initialized,
            MissingFiles = missingFiles
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to initialize event data: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/message", (MessageRequest request) =>
{
    Console.WriteLine(
        $"Received from {request.ClientName}: {request.Message}");

    return Results.Ok(new MessageResponse
    {
        Success = true,
        Message = $"Server received: {request.Message}",
        ServerTime = DateTime.Now
    });
});

app.MapGet("/api/event_list", () =>
{
    var dataPath = builder.Configuration["DataPath"];

    if (string.IsNullOrWhiteSpace(dataPath))
    {
        return Results.Problem("DataPath is not configured.", statusCode: StatusCodes.Status500InternalServerError);
    }

    var fullDataPath = Path.GetFullPath(dataPath);
    Directory.CreateDirectory(fullDataPath);

    var eventListPath = Path.Combine(fullDataPath, "EventsList.json");

    if (!File.Exists(eventListPath))
    {
        return Results.NotFound(new { error = "EventList.json was not found." });
    }

    try
    {
        var json = File.ReadAllText(eventListPath);
        var eventList = JsonSerializer.Deserialize<EventListModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (eventList is null)
        {
            return Results.Problem("Unable to deserialize EventList.json.", statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(eventList);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to read EventList.json: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/notebook_list/{eventId}", (string eventId) =>
{
    var dataPath = builder.Configuration["DataPath"];
    if (string.IsNullOrWhiteSpace(dataPath))
    {
        return Results.Problem("DataPath is not configured.", statusCode: StatusCodes.Status500InternalServerError);
    }
    var fullDataPath = Path.GetFullPath(dataPath);
    Directory.CreateDirectory(fullDataPath);
    var notebookFilePattern = $"DigitalEngineeringNotebooks-{eventId}*.json";
    var notebookFiles = Directory.EnumerateFiles(fullDataPath, notebookFilePattern, SearchOption.TopDirectoryOnly).ToList();
    if (notebookFiles.Count == 0)
    {
        return Results.NotFound(new { error = $"No notebook files found for event ID '{eventId}'." });
    }
    try
    {
        var json = File.ReadAllText(notebookFiles[0]);
        var notebookListModel = JsonSerializer.Deserialize<NotebookListModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return Results.Ok(notebookListModel);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to read notebook files: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
    }
});

static void ConvertWorkbookToJson(string workbookPathFile)
{
    var jsonFilePath = Path.ChangeExtension(workbookPathFile, ".json");
    if (File.Exists(jsonFilePath))
    {
        return;
    }

    try
    {
        var csvLines = File.ReadAllLines(workbookPathFile);
        if (csvLines.Length == 0)
        {
            return;
        }

        var headers = ParseCsvLine(csvLines[0]);
        var headerMap = headers
            .Select((header, index) => new { header = header.Trim(), index })
            .Where(x => !string.IsNullOrWhiteSpace(x.header))
            .ToDictionary(x => x.header, x => x.index, StringComparer.OrdinalIgnoreCase);

        var notebookList = new List<NotebookItemModel>();
        var gradeLevel = GradeLevel.Elementary;

        for (var i = 1; i < csvLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(csvLines[i]))
            {
                continue;
            }

            var values = ParseCsvLine(csvLines[i]);
            if (values.Count == 0)
            {
                continue;
            }

            var teamNumber = GetCsvValue(values, headerMap, "team_number", "team", "team number");
            var link = GetCsvValue(values, headerMap, "notebook_link", "link", "notebook link");
            var lastUpdatedValue = GetCsvValue(values, headerMap, "updated_at", "last_updated", "updated at");
            var gradeValue = GetCsvValue(values, headerMap, "grade_level", "grade level");

            var item = new NotebookItemModel
            {
                NotebookId = Guid.NewGuid(),
                TeamNumber = teamNumber,
                Link = link,
                LastUpdated = TryParseDate(lastUpdatedValue)
            };

            if (!string.IsNullOrWhiteSpace(gradeValue) && Enum.TryParse<GradeLevel>(gradeValue, true, out var parsedGrade))
            {
                gradeLevel = parsedGrade;
            }

            notebookList.Add(item);
        }

        var model = new NotebookListModel
        {
            EventGradeLevel = gradeLevel,
            NotebookList = notebookList
        };

        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonFilePath, json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to convert workbook {workbookPathFile} to JSON: {ex.Message}");
    }
}

static string GetCsvValue(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headerMap, params string[] candidateHeaders)
{
    foreach (var candidate in candidateHeaders)
    {
        if (headerMap.TryGetValue(candidate, out var index) && index < values.Count)
        {
            return values[index].Trim();
        }
    }

    return string.Empty;
}

static DateTime TryParseDate(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return DateTime.MinValue;
    }

    return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result)
        ? result
        : DateTime.MinValue;
}

static List<string> ParseCsvLine(string line)
{
    var result = new List<string>();
    var current = new StringBuilder();
    var inQuotes = false;

    for (var i = 0; i < line.Length; i++)
    {
        var ch = line[i];

        if (ch == '"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else
            {
                inQuotes = !inQuotes;
            }
        }
        else if (ch == ',' && !inQuotes)
        {
            result.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(ch);
        }
    }

    result.Add(current.ToString());
    return result;
}

app.Run();

