using System.Diagnostics;
using System.Xml.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Relawan.Web.Datas;
using Relawan.Web.Models;
using Relawan.Web.ViewModels;

namespace Relawan.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _context = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var dataOrganRelawan = await _context.Relawans.ToListAsync();
        
        return View(dataOrganRelawan);
    }
    
    public IActionResult ImportData(string message = "")
    {
        ViewBag.Message = message;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ImportData([FromForm] ImportDataViewModel data)
    {
        if (data.PeopleFile != null && data.PeopleFile.Length > 0)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = new MemoryStream();
            await data.PeopleFile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            var newRelawan = new Relawan.Web.Datas.Relawan
            {
                Name = data.Name,
                GroupName = data.GroupName,
                IdentityNumber = data.IdentityNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = data.Name,
            };

            var people = new List<Person>();
            var peopleHistory = new List<PersonHistory>();
            var emptyRow = 0;
            for (int row = 2; row <= rowCount; row++)
            {
                if(emptyRow >= 3){
                    break;
                }

                var person = new Person
                {
                    Kecamatan = worksheet.Cells[row, 2].Value?.ToString().Trim().ToUpper(),
                    Desa = worksheet.Cells[row, 3].Value?.ToString().Trim().ToUpper(),
                    Name = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    IdentityNumber = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    Dusun = worksheet.Cells[row, 7].Value?.ToString().Trim().ToUpper(),
                    RT = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    RW = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    NoHP = "",
                    CreatedBy = data.Name,
                    CreatedAt = DateTime.UtcNow
                };
                // Check if all property in person null skip
                if (person.Kecamatan == null 
                && person.Desa == null 
                && person.IdentityNumber == null 
                && person.Name == null)
                {
                    emptyRow++;
                    continue;
                }

                if(string.IsNullOrEmpty(person.Name))
                {
                    continue;
                }

                if(string.IsNullOrEmpty(person.Kecamatan))
                {
                    continue;
                }
                
                person.IdentityNumber ??= string.Empty;
                person.Desa ??= string.Empty;

                people.Add(person);
            }

            if(emptyRow >= 3 && people.Count == 0) {
                return RedirectToAction("ImportData", new { 
                    message = "Data yang diupload kosong atau banyak data yang kosong, mohon untuk diperbaiki terlebih dahulu" 
                });
            }

            await _context.People.AddRangeAsync(people);
            // check if relawan not exist by identity number
            if(!await _context.Relawans.AnyAsync(x => x.IdentityNumber == newRelawan.IdentityNumber))
            {
                await _context.Relawans.AddAsync(newRelawan);    
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Report");
        }
        return View();
    }

    public async Task<IActionResult> DataGanda(){
        var data = await _context.People.GroupBy(x => new { x.Kecamatan, x.Desa, x.Name }).Where(x => x.Count() > 1).CountAsync();

        ViewBag.DataGanda = data;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDataGanda([FromQuery] DataTablesRequest request)
    {
        // Base query
        var query = _context.People.AsQueryable();

        // Apply search filter
        if(!string.IsNullOrEmpty(request.SearchValue))
        {
            query = query.Where(p => 
                (p.CreatedBy != null && p.CreatedBy.Contains(request.SearchValue)) ||
                p.Kecamatan.Contains(request.SearchValue) ||
                p.Desa.Contains(request.SearchValue) ||
                p.Name.Contains(request.SearchValue) ||
                p.IdentityNumber.Contains(request.SearchValue));
        }

        var totalRecords = await (from a in query
        group a by new { a.Kecamatan, a.Desa, a.Name } into duplicateValue
        where duplicateValue.Count() > 1
        select new PersonViewModel {
            Id = duplicateValue.FirstOrDefault().Id
        }
        ).CountAsync();

        var data = await (from a in query
        group a by new { a.Kecamatan, a.Desa, a.Name } into duplicateValue
        where duplicateValue.Count() > 1
        select new PersonViewModel {
            Id = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().Id,
            Kecamatan = duplicateValue.Key.Kecamatan,
            Desa = duplicateValue.Key.Desa,
            Name = duplicateValue.Key.Name,
            IdentityNumber = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().IdentityNumber,
            Dusun = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().Dusun,
            RT = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().RT,
            RW = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().RW,
            NoHP = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().NoHP,
            CreatedAt = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().CreatedAt,
            CreatedBy = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().CreatedBy,
            DuplicateCount = duplicateValue.Count(),
            History = duplicateValue.OrderBy( x=>x.CreatedAt).Select( x=> new PeopleHistoryData {
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy
            }).ToList()
        }
        ).Skip(request.Start)
        .Take(request.Length)
        .ToListAsync();

        // Prepare DataTables response
        var response = new DataTablesResponse
        {
            Draw = request.Draw,
            RecordsTotal = totalRecords,
            RecordsFiltered = totalRecords, // Adjust if additional filtering is added
            Data = data
        };

        return Json(response);
    }

    public async Task<IActionResult> Report()
    {
        var totalKecamatan = await _context.People.GroupBy( x => x.Kecamatan).CountAsync();
        var totalDesa = await _context.People.GroupBy( x => new { x.Kecamatan, x.Desa }).CountAsync();
        var total = await _context.People.CountAsync();
        var totalDuplicate = await _context.People.GroupBy(x => new { x.Kecamatan, x.Desa, x.Name }).Where(x => x.Count() > 1).CountAsync();
        var totalOrgan = await _context.Relawans.GroupBy(x  => x.GroupName).CountAsync();

        ViewBag.totalKecamatan = totalKecamatan;
        ViewBag.totalDesa = totalDesa;
        ViewBag.total = total;
        ViewBag.totalDuplicate = totalDuplicate;
        ViewBag.totalOrgan = totalOrgan;

        return View();
    }
    [HttpGet]
    
    public async Task<IActionResult> GetPeople([FromQuery] DataTablesRequest request)
    {
        // Base query
        var query = _context.People.AsQueryable();

        // Apply search filter
        if(!string.IsNullOrEmpty(request.SearchValue))
        {
            query = query.Where(p => 
                (p.CreatedBy != null && p.CreatedBy.Contains(request.SearchValue)) ||
                p.Kecamatan.Contains(request.SearchValue) ||
                p.Desa.Contains(request.SearchValue) ||
                p.Name.Contains(request.SearchValue) ||
                p.IdentityNumber.Contains(request.SearchValue));
        }

        // Total records
        var totalRecords = await query.CountAsync();

        // Paging
        var data = await (from a in query
        group a by new { a.Kecamatan, a.Desa, a.Name, a.IdentityNumber } into duplicateValue
        select new PersonViewModel {
            Id = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().Id,
            Kecamatan = duplicateValue.Key.Kecamatan,
            Desa = duplicateValue.Key.Desa,
            Name = duplicateValue.Key.Name,
            IdentityNumber = duplicateValue.Key.IdentityNumber,
            Dusun = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().Dusun,
            RT = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().RT,
            RW = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().RW,
            NoHP = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().NoHP,
            CreatedAt = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().CreatedAt,
            CreatedBy = duplicateValue.OrderBy( x=>x.CreatedAt).FirstOrDefault().CreatedBy,
            DuplicateCount = duplicateValue.Count(),
            History = duplicateValue.OrderBy( x=>x.CreatedAt).Select( x=> new PeopleHistoryData {
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy
            }).ToList()
        }
        ).Skip(request.Start)
        .Take(request.Length)
        .ToListAsync();

        // Prepare DataTables response
        var response = new DataTablesResponse
        {
            Draw = request.Draw,
            RecordsTotal = totalRecords,
            RecordsFiltered = totalRecords, // Adjust if additional filtering is added
            Data = data
        };

        return Json(response);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
