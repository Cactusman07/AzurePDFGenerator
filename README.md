# AzurePDFGenerator
A repository for an Azure C# function app that generates a PDF and stores it in your blob storage account.

I worked based off this tutorial: https://itnext.io/to-azure-functions-wkhtmltopdf-convert-html-to-pdf-9dc69bcd843b


However, there were a few additions that I needed to make to ensure that the application wasn't failing after one use (subsequent uses returned 503 errors).

I've detailed these here: https://sammuir.co.nz/blog/azure-function-app-c-pdf-generation/

Change summary:
1. Use Dependency Injection
2. Use a local.settings.json file to use connection strings locally (and where to add & how to access via the function app application settings in the Azure Portal)
3. Make sure that certain packages don't use certain versions.