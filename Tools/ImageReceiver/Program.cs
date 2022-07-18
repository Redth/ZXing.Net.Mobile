var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var picNum = 0;
var ignoreCount = 0;
var saveAfterAmount = 20;

var imagePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "Images");
Directory.CreateDirectory(imagePath);
Console.WriteLine($"Saving images to: {imagePath}");


app.MapPost("/", async context =>
{
    if (ignoreCount >= saveAfterAmount)
    {
        using (var fs = File.Create(Path.Combine(imagePath, $"File_{picNum}.bmp")))
        {
            picNum++;
            await context.Request.Body.CopyToAsync(fs);
        }
        ignoreCount = 0;
    }

    ignoreCount++;

    context.Response.StatusCode = 200;
});

app.Run();