namespace Results.Tests.Models;

/// <summary>
/// Person model for mocking reference type data in tests.
/// </summary>
/// <param name="Age"></param>
/// <param name="Name"></param>
public record Person(int Age, string Name);
