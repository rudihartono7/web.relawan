namespace Relawan.Web.ViewModels;
public class ImportDataViewModel
{
    public string GroupName { get; set; } = null!;
    public string Name {get; set; } = null!;
    public string IdentityNumber { get; set; } = null!;
    public IFormFile? PeopleFile { get; set; }
}
