namespace F1Widget.Extensions;

public static class StringExtensions
{
    public static string CapitalizeFirstLetter(this string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            : char.ToUpper(input[0]) + input[1..];
    }
}