namespace Cosmos_Epub_Reader_Lib
{
    public class EpubChapter
    {
        public string Title { get; set; } = string.Empty;  // Title of the chapter
        public string Content { get; set; } = string.Empty;  // HTML content of the chapter
        public string? FilePath { get; set; }  // Path to the chapter file within the EPUB

        // Method to validate that required fields are properly set
        public bool IsValid(out string validationMessage)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                validationMessage = "Chapter title is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Content))
            {
                validationMessage = "Chapter content is missing.";
                return false;
            }

            validationMessage = "Chapter is valid.";
            return true;
        }

        // Override ToString method for better readability
        public override string ToString()
        {
            return $"Title: {Title}, FilePath: {FilePath ?? "N/A"}";
        }
    }
}
