using System;
using System.Collections.Generic;

namespace Cosmos_Epub_Reader_Lib
{
    public class EpubMetadata
    {
        public string Title { get; set; } = string.Empty;        // Title of the EPUB
        public string Author { get; set; } = string.Empty;       // Author of the EPUB
        public string Publisher { get; set; } = string.Empty;    // Publisher information
        public DateTime? PublicationDate { get; set; }           // Optional publication date
        public string Language { get; set; } = string.Empty;     // Language of the EPUB
        public string Identifier { get; set; } = string.Empty;   // Identifier (e.g., ISBN)
        public string Description { get; set; } = string.Empty;  // Description or summary

        // Validate that required fields are properly set
        public bool Validate(out List<string> validationErrors)
        {
            validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(Title))
                validationErrors.Add("Title is required.");

            if (string.IsNullOrWhiteSpace(Author))
                validationErrors.Add("Author is required.");

            if (string.IsNullOrWhiteSpace(Identifier))
                validationErrors.Add("Identifier (e.g., ISBN) is recommended.");

            return validationErrors.Count == 0;
        }

        // Override ToString method for better readability
        public override string ToString()
        {
            return $"Title: {Title}\n" +
                   $"Author: {Author}\n" +
                   $"Publisher: {Publisher}\n" +
                   $"Publication Date: {PublicationDate?.ToShortDateString() ?? "N/A"}\n" +
                   $"Language: {Language}\n" +
                   $"Identifier: {Identifier}\n" +
                   $"Description: {Description}\n";
        }
    }
}