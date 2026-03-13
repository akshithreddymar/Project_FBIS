namespace TransactionIngest.Configuration;

public class AppSettings
{
    public ApiSettings Api { get; set; } = new();

    public ConnectionStringsSettings ConnectionStrings { get; set; } = new();
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    public string MockFeedPath { get; set; } = string.Empty;

    public bool UseMockFeed { get; set; }
}

public class ConnectionStringsSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}