namespace AcademicRecords.Shared.Algebra;

public sealed record TransformationStep(
    int Step,
    string Rule,
    string Before,
    string After,
    string Reason);
