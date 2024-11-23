using System.ComponentModel.DataAnnotations;

namespace Relawan.Web.Datas;
public class Person {
    public int Id { get; set; }

    [Required]
    public string Kecamatan { get; set; } = null!;

    [Required]
    public string Desa { get; set; } = null!;

    [Required]
    public string IdentityNumber { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Dusun { get; set; }
    public string? RT { get; set; }
    public string? RW { get; set; }
    public string? NoHP { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}