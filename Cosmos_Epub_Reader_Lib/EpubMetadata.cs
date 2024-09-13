using System;
using System.Collections.Generic;

namespace Cosmos_Epub_Reader_Lib
{
    /// <summary>
    /// Represents the metadata of an EPUB file, including title, author, publisher, and other descriptive information.
    /// </summary>
    public class EpubMetadata
    {
        /// <summary>
        /// Gets or sets the title of the EPUB.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the author of the EPUB.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the publisher information of the EPUB.
        /// </summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional publication date of the EPUB.
        /// </summary>
        public DateTime? PublicationDate { get; set; }

        /// <summary>
        /// Gets or sets the language of the EPUB.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the EPUB, such as ISBN.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description or summary of the EPUB.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Validates the metadata fields to ensure that required fields are properly set.
        /// </summary>
        /// <param name="validationErrors">A list of validation error messages if any required fields are missing.</param>
        /// <returns>True if the metadata is valid; otherwise, false.</returns>
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

        /// <summary>
        /// Returns a string representation of the metadata for better readability.
        /// </summary>
        /// <returns>A formatted string displaying the metadata details.</returns>
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