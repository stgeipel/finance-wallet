using System.Reflection;

namespace DatabaseMigration;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}