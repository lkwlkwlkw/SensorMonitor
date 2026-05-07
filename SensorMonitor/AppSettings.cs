namespace SensorMonitor;

    public class AppSettings
{
    public string ConnectionAddress { get; set; }
    public int PLCPollingInterval { get; set; }
    public int SaveToDBInterval { get; set; }
    public NodeIds NodeIds { get; set; }
}

public class NodeIds
{
    public string T1 { get; set; }
    public string T2 { get; set; }
    public string T3 { get; set; }
    public string T4 { get; set; }
    public string T5 { get; set; }
    public string T6 { get; set; }
    public string T7 { get; set; }
    public string T8 { get; set; }
    public string T9 { get; set; }
    public string T10 { get; set; }
    public string T11 { get; set; }
    public string T12 { get; set; }
    public string P1 { get; set; }
    public string P2 { get; set; }
    public string W1 { get; set; }
}
