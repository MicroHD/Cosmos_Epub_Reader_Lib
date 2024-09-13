namespace Cosmos_Epub_Reader_Lib
{
    /// <summary>
    /// Represents a chapter within an EPUB file, including its title, content, and file path.
    /// </summary>
    public class EpubChapter
    {
        /// <summary>
        /// Gets or sets the title of the chapter.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML content of the chapter.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path to the chapter file within the EPUB.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Validates the chapter to ensure that required fields are properly set.
        /// </summary>
        /// <param name="validationMessage">A message describing the validation result.</param>
        /// <returns>True if the chapter is valid; otherwise, false.</returns>
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

        /// <summary>
        /// Returns a string representation of the chapter for better readability.
        /// </summary>
        /// <returns>A formatted string displaying the chapter's title and file path.</returns>
        public override string ToString()
        {
            return $"Title: {Title}, FilePath: {FilePath ?? "N/A"}";
        }
    }
}