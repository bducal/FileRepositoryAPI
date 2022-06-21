using Microsoft.AspNetCore.Mvc;
using FileRepositoryAPI.Services;
using FileRepositoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Identity.Web.Resource;
using Newtonsoft.Json;

//Nice tutorial: https://www.mongodb.com/developer/how-to/create-restful-api-dotnet-core-mongodb/v

namespace FileRepositoryAPI.Controllers;

[Authorize]
[Controller]
[Route("api/[controller]")]
public class FileListController : Controller
{
    private readonly MongoDBService _mongoDBService;
    public FileListController(MongoDBService mongoDBService)
    {
        _mongoDBService = mongoDBService;
    }

    static readonly string[] scopeRequiredByApi = new string[] { "access_as_user" };

    public class Config
    {
        public string Title;
        public string useAuthentication;
    }
    private bool authorized = false;
    private string? userName = "";

    [Authorize]
    private void AuthorizeCheck(){}

    [AllowAnonymous]
    private bool isAuthorized()
    {
        authorized = false;
        try
        {
            StreamReader r = new StreamReader("config.json");
            Config config = JsonConvert.DeserializeObject<Config>(r.ReadToEnd());
            if (config.useAuthentication != "False")
            {
                AuthorizeCheck();
                HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
                userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                authorized = true;
            }
            else
                authorized = true;
        }
        catch{ }
        return authorized;
    }

    [HttpGet]
    [Route("ConfigInfo")]
    [AllowAnonymous]
    public async Task<string> ConfigInfo()
    {
        StreamReader r = new StreamReader("config.json");
        Config config = JsonConvert.DeserializeObject<Config>(r.ReadToEnd());
        return "Title: "+config.Title + " Authentication:" + config.useAuthentication;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<List<FileList>> Get()
    {
        if (isAuthorized())
            return await _mongoDBService.GetAsyncAll();
        else
            return null;
    }


    [HttpGet]
    [Route("Download/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFile(string id)
    {
        if (isAuthorized())
        {
            byte[] dataBytes = await _mongoDBService.DownloadFileAsync(id);
            //var dataBytes = System.IO.File.ReadAllBytes("d:\\temp\\test.png"); 
            var dataStream = new MemoryStream(dataBytes);
            return File(dataStream, "application/octet-stream", null);
        }
        else
            return null;
    }

    public class FileWihtAttributes
    {
        public string FileID { get; set; }
        public string FileTags { get; set; }
        public string FileOwner { get; set; }
        public IFormFile File { get; set; }
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    [AllowAnonymous]
    public async Task<IActionResult> PostFile([FromForm] FileWihtAttributes file)
    {
        if (isAuthorized())
        {
            long size = file.File.Length;
            var filePath = Path.GetTempFileName();
            // using (var stream = System.IO.File.Create("d:\\temp\\" + file.File.FileName))
            // {
            //     await file.File.CopyToAsync(stream);
            // }

            await _mongoDBService.UploadFileAsync(file.File, file.FileID);
            FileList fl = new FileList();
            fl.FileName = file.File.FileName;
            //fl.FileTags=file.FileTags;
            fl.FileOwner = file.FileOwner;
            fl.Id = file.FileID;
            await _mongoDBService.UpdateAsync(fl);
            return Ok(new { count = 1, size });
        }
        else
            return null;
    }


    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<List<FileList>> Get(string id)
    {
        if (isAuthorized())
            return await _mongoDBService.GetAsync(id);
        else
            return null;
    }


    [HttpPut("{id}")] //upsert
    [AllowAnonymous]
    public async Task<List<FileList>> Upsert(string id, [FromBody] FileList fl)
    {
        if (isAuthorized())
        {
            if (id == "NEW")
            {
                List<FileList> _fl = new List<FileList>();
                _fl.Add(await _mongoDBService.CreateAsync(fl));
                return _fl;
            }
            else
            {
                await _mongoDBService.UpdateAsync(fl);
                return await _mongoDBService.GetAsync(fl.Id);
            }
        }
        else
            return null;
    }

    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<List<FileList>> Delete(string id)
    {
        if (isAuthorized())
        {
            await _mongoDBService.DeleteAsync(id);
            return await _mongoDBService.GetAsyncAll();
        }
        else
            return null;
    }
}