using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DinkToPdf;
using IPdfConverter = DinkToPdf.Contracts.IConverter;

[assembly: FunctionsStartup(typeof(pdfCreation.Startup))]

namespace pdfCreation
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(typeof(IPdfConverter), new SynchronizedConverter(new PdfTools()));
        }
    }

    public class Html2Pdf
    {
        // Read more about converter on: https://github.com/rdvojmoc/DinkToPdf
        // For our purposes we are going to use SynchronizedConverter
        //IPdfConverter pdfConverter = new SynchronizedConverter(new PdfTools());

        //Note we are using SynchronisedConverter / IPDFConverter via dependency injection
        private readonly IPdfConverter pdfConverter;
        public Html2Pdf(IPdfConverter pdfConverter)
        {
            this.pdfConverter = pdfConverter;
        }

        // A function to convert html content to pdf based on the configuration passed as arguments
        // Arguments:
        // HtmlContent: the html content to be converted
        // Margins: the margis around the content
        // DPI: The dpi is very important when you want to print the pdf.
        // Returns a byte array of the pdf which can be stored as a file
        private byte[] BuildPdf(string HtmlContent, MarginSettings Margins, int? DPI = 180)
        {
            // Call the Convert method of IPdfConverter / SynchronisedConverter "pdfConverter"
            return pdfConverter.Convert(new HtmlToPdfDocument()
            {
                // Set the html content
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = HtmlContent,
                        WebSettings = { DefaultEncoding = "UTF-8", LoadImages = true }
                    }
                },
                // Set the configurations
                GlobalSettings = new GlobalSettings
                {
                    PaperSize = PaperKind.A4,
                    DPI = DPI,
                    Margins = Margins
                }
            });
        }

        private string ConnectionString(ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connString = config.GetConnectionString("ConnectionString");            

            if(connString == null){log.LogInformation("Connection String is null");}

            return connString;
        }

        // The first arugment tells that the functions can be triggerd by a POST HTTP request. 
        // The second argument is mainly used for logging information, warnings or errors
        [FunctionName("Html2Pdf")]
        [STAThread]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "POST")] Html2PdfRequest Request, ILogger Log, ExecutionContext Context)
        {
            try{
                Log.LogInformation("C# HTTP trigger function started for HTML2PDF.");
                Log.LogInformation(Request.HtmlContent);

                // PDFByteArray is a byte array of pdf generated from the HtmlContent 
                var PDFByteArray = BuildPdf(Request.HtmlContent, new MarginSettings(2, 2, 2, 2));

                // The connection string of the Storage Account to which our PDF file will be uploaded
                var StorageConnectionString = ConnectionString(Log, Context);

                // Generate an instance of CloudStorageAccount by parsing the connection string
                var StorageAccount = CloudStorageAccount.Parse(StorageConnectionString);

                // Create an instance of CloudBlobClient to connect to our storage account
                CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

                // Get the instance of CloudBlobContainer which points to a container name "pdf"
                // Replace your own container name
                CloudBlobContainer BlobContainer = BlobClient.GetContainerReference("pdf");
                
                // Get the instance of the CloudBlockBlob to which the PDFByteArray will be uploaded
                CloudBlockBlob Blob = BlobContainer.GetBlockBlobReference(Request.PDFFileName);
                
                Log.LogInformation("Attempting to upload " + Request.PDFFileName);
                // Upload the pdf blob
                await Blob.UploadFromByteArrayAsync(PDFByteArray, 0, PDFByteArray.Length);

                return (ActionResult)new OkObjectResult(Request.PDFFileName + " was successfully created.");
            }
            catch(Exception e)
            {
                Log.LogInformation("Error occurred: " + e);
                return (ActionResult)new OkObjectResult("Error" + e.Message);
            }
            
        }
    }
}

