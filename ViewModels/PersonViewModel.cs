namespace Relawan.Web.ViewModels;

public class PersonViewModel {
    public int Id { get; set; }
    public string Kecamatan { get; set; } = null!;
    public string Desa { get; set; } = null!;
    public string IdentityNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Dusun { get; set; }
    public string? RT { get; set; }
    public string? RW { get; set; }
    public string? NoHP { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int DuplicateCount { get; set; }
    public List<PeopleHistoryData>? History { get; set; }
    public List<string> HistoryList  => History == null || History.Count == 0 ? new List<string>() : History.Select( x => $"{x.CreatedBy} - {x.CreatedAt}").ToList();
    public string HistoryDesc => string.Join("\n", HistoryList);
}

public class PeopleHistoryData {
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
}