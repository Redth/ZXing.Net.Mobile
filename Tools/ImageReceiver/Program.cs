
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var picNum = 0;

var imagePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "Images");
Directory.CreateDirectory(imagePath);
Console.WriteLine($"Saving images to: {imagePath}");


app.MapPost("/", async context =>
{
    try
    {
        var filePrefix = "File";
        if (context.Request.Headers.TryGetValue("FileName", out var newFilePrefix))
        {
            filePrefix = newFilePrefix;
        }

        using (var fs = File.Create(Path.Combine(imagePath, $"{filePrefix}_{picNum}.bmp")))
        {
            picNum++;
            await context.Request.Body.CopyToAsync(fs);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to save File_{picNum - 1}.bmp");
        Console.Error.WriteLine(ex.Message);
    }

    context.Response.StatusCode = 200;
});

app.Run();