using TheSampleApi.Data;

namespace TheSampleApi.Endpoints;

public static class CourseEndpoints
{
    public static void AddCourseEndpoints(this WebApplication app)
    {
        app.MapGet("/courses", LoadAllCoursesAsyn);
        app.MapGet("/courses/{id}", LoadCourseByIdAsyn);
    }

    private static async Task<IResult> LoadCourseByIdAsyn(CourseData data, int id, int? delay)
    {
        var output = data.Courses.FirstOrDefault(c => c.Id == id);

        if (delay is not null) //testing
        {
            //max delay of 5 mins, 300,000 milliseconds
            if (delay > 300_000)
            {
                delay = 300_000;
            }

            //simulate a delay  
            await Task.Delay(delay.Value);
        }

        if (output is null)
        {
            return Results.NotFound();
        }
        return Results.Ok(output);
    }

    private static async Task<IResult> LoadAllCoursesAsyn(CourseData data, string? courseType, string? search, int? delay)
    {
        var output = data.Courses;
        if ( !string.IsNullOrWhiteSpace(courseType) )
        {            
            output.RemoveAll(x => string.Compare(x.CourseType, courseType, StringComparison.OrdinalIgnoreCase) != 0);
        }
        
        if (!string.IsNullOrWhiteSpace(search)) {
            output.RemoveAll(x => !x.CourseName.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                            !x.ShortDescription.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if ( delay is not null) //testing
        {
            //max delay of 5 mins, 300,000 milliseconds
            if (delay > 300_000)
            {
                delay = 300_000;
            }

            //simulate a delay  
            await Task.Delay(delay.Value);
        }
        return Results.Ok(output);
    }
}
