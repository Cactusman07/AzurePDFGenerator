namespace pdfCreation
{
    public class Html2PdfRequest
    {
        // The HTML content that needs to be converted.
        public string HtmlContent { get; set; }
      
        // The name of the PDF file to be generated
        public string PDFFileName { get; set; }
    }
}