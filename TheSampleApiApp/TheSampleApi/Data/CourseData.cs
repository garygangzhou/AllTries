using System.Text.Json;
using TheSampleApi.Models;

namespace TheSampleApi.Data;

public class CourseData
{
    public List<CourseModel> Courses { get; private set; }

    public CourseData()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "coursedata.json");

        string json = File.ReadAllText(filePath);
        Courses = JsonSerializer.Deserialize<List<CourseModel>>(json, options) ?? new();
    }
}
