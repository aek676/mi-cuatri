using backend.Enums;

namespace backend.Tests.Unit.Enums;

/// <summary>
/// Unit tests for CalendarCategory enum.
/// Tests enum values, count, and string serialization/deserialization.
/// </summary>
public class CalendarCategoryTests
{
    #region Enum Values Tests

    [Fact]
    public void CalendarCategory_HasCourseValue()
    {
        CalendarCategory.Course.Should().Be(CalendarCategory.Course);
    }

    [Fact]
    public void CalendarCategory_HasGradebookColumnValue()
    {
        CalendarCategory.GradebookColumn.Should().Be(CalendarCategory.GradebookColumn);
    }

    [Fact]
    public void CalendarCategory_HasInstitutionValue()
    {
        CalendarCategory.Institution.Should().Be(CalendarCategory.Institution);
    }

    [Fact]
    public void CalendarCategory_HasOfficeHoursValue()
    {
        CalendarCategory.OfficeHours.Should().Be(CalendarCategory.OfficeHours);
    }

    [Fact]
    public void CalendarCategory_HasPersonalValue()
    {
        CalendarCategory.Personal.Should().Be(CalendarCategory.Personal);
    }

    #endregion

    #region Enum Count Tests

    [Fact]
    public void CalendarCategory_HasExactlyFiveValues()
    {
        var values = Enum.GetValues(typeof(CalendarCategory));

        values.Length.Should().Be(5);
    }

    [Fact]
    public void CalendarCategory_AllValuesAreUnique()
    {
        var values = Enum.GetValues(typeof(CalendarCategory)).Cast<int>().ToList();

        var distinctValues = values.Distinct().ToList();

        values.Count.Should().Be(distinctValues.Count);
    }

    #endregion

    #region String Conversion Tests

    [Theory]
    [InlineData(CalendarCategory.Course, "Course")]
    [InlineData(CalendarCategory.GradebookColumn, "GradebookColumn")]
    [InlineData(CalendarCategory.Institution, "Institution")]
    [InlineData(CalendarCategory.OfficeHours, "OfficeHours")]
    [InlineData(CalendarCategory.Personal, "Personal")]
    public void CalendarCategory_ToString_ReturnsCorrectString(CalendarCategory category, string expected)
    {
        var result = category.ToString();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Course", CalendarCategory.Course)]
    [InlineData("GradebookColumn", CalendarCategory.GradebookColumn)]
    [InlineData("Institution", CalendarCategory.Institution)]
    [InlineData("OfficeHours", CalendarCategory.OfficeHours)]
    [InlineData("Personal", CalendarCategory.Personal)]
    public void CalendarCategory_Parse_WithValidString_ReturnsCorrectValue(string input, CalendarCategory expected)
    {
        var result = Enum.Parse<CalendarCategory>(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalendarCategory_Parse_WithInvalidString_ThrowsArgumentException()
    {
        var act = () => Enum.Parse<CalendarCategory>("InvalidCategory");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalendarCategory_TryParse_WithValidString_ReturnsTrue()
    {
        var result = Enum.TryParse<CalendarCategory>("Course", out var category);

        result.Should().BeTrue();
        category.Should().Be(CalendarCategory.Course);
    }

    [Fact]
    public void CalendarCategory_TryParse_WithInvalidString_ReturnsFalse()
    {
        var result = Enum.TryParse<CalendarCategory>("InvalidCategory", out _);

        result.Should().BeFalse();
    }

    #endregion

    #region Numeric Value Tests

    [Theory]
    [InlineData(CalendarCategory.Course, 0)]
    [InlineData(CalendarCategory.GradebookColumn, 1)]
    [InlineData(CalendarCategory.Institution, 2)]
    [InlineData(CalendarCategory.OfficeHours, 3)]
    [InlineData(CalendarCategory.Personal, 4)]
    public void CalendarCategory_HasCorrectNumericValue(CalendarCategory category, int expectedValue)
    {
        var numericValue = (int)category;

        numericValue.Should().Be(expectedValue);
    }

    #endregion
}
