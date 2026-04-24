namespace CsvImporter.Core.Models;

public class ConnectionProfile
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Name        { get; set; } = string.Empty;
    public DbProvider Provider { get; set; }
    public string Host        { get; set; } = string.Empty;
    public int    Port        { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DbName      { get; set; } = string.Empty;
    public string Username    { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
    public DbRole Role        { get; set; } = DbRole.Default;
    public bool   IsDefault   { get; set; } = false;
}

public enum DbProvider { Oracle, MsSql, Postgres }
public enum DbRole     { Default, SysDba, SysOper }
