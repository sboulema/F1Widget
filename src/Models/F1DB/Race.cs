namespace F1Widget.Models.F1DB;

public class Race
{
    public string CircuitLayoutId { get; set; } = string.Empty;

    public string OfficialName { get; set; } = string.Empty;

    public DateOnly? Date { get; set; }

    public TimeOnly? Time { get; set; }

    public DateOnly? FreePractice1Date { get; set; }

    public TimeOnly? FreePractice1Time { get; set; }

    public DateOnly? FreePractice2Date { get; set; }

    public TimeOnly? FreePractice2Time { get; set; }

    public DateOnly? FreePractice3Date { get; set; }

    public TimeOnly? FreePractice3Time { get; set; }

    public DateOnly? QualifyingDate { get; set; }

    public TimeOnly? QualifyingTime { get; set; }

    public DateOnly? SprintQualifyingDate { get; set; }

    public TimeOnly? SprintQualifyingTime { get; set; }

    public DateOnly? SprintRaceDate { get; set; }

    public TimeOnly? SprintRaceTime { get; set; }
}