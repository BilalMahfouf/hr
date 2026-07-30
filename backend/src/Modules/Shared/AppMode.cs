namespace Shared;

public enum AppMode
{
    Dev = 1,
    Production = 2
}
public static class AppSettings
{
    public static AppMode appMode = AppMode.Production;

    public static readonly string ProductionConnectionStringName
        = "ConnectionStrings__Default";

    public static readonly string DevConnectionStringName
        = "DefaultConnectionLocal";

}
