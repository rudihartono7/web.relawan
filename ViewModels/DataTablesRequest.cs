using Microsoft.AspNetCore.Mvc;

namespace Relawan.Web.ViewModels;
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    [BindProperty(Name = "search[value]")]
    public string SearchValue { get; set; } = string.Empty;
    public DataTablesSearch Search { get; set; } = new DataTablesSearch();
}

public class DataTablesSearch
{
    public string Value { get; set; } = string.Empty;
    public bool Regex { get; set; }
}

public class DataTablesResponse
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public object Data { get; set; } = null!;
}