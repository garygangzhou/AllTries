namespace SampleTestUI.Models;

public class CourseModel
{
    public int Id { get; set; }
    public bool IsPreorder { get; set; }
    public required string CourseUrl { get; set; }
    public required string CourseType { get; set; }
    public required string CourseName { get; set; }
    public int CourseLessonCount { get; set; }
    public double CourseLengthInHours { get; set; }
    public required string ShortDescription { get; set; }
    public required string CourseImage { get; set; }
}
