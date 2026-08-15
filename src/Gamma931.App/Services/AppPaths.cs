namespace Gamma931.App.Services;

public static class AppPaths
{
    /// <summary>
    /// Where card CSVs live at runtime. The .csproj copies /Data next to the built exe
    /// (see Gamma931.App.csproj), so this is always resolvable from a published/run app.
    /// </summary>
    public static string CardDataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");
}
