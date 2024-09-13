using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Cosmos_Epub_Reader_Lib
{
    /// <summary>
    /// Provides functionality to read and parse EPUB files, extracting metadata and chapters.
    /// </summary>
    public class EpubReader
    {
        /// <summary>
        /// Opens and reads an EPUB file, extracting its metadata and chapters.
        /// </summary>
        /// <param name="filePath">The path to the EPUB file to be read.</param>
        /// <returns>An <see cref="EpubFile"/> object containing the parsed content of the EPUB file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified EPUB file does not exist.</exception>
        /// <exception cref="Exception">Thrown if the EPUB file structure is invalid or if parsing fails.</exception>
        public EpubFile OpenEpub(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified EPUB file does not exist.");

            // Extract the EPUB file (ZIP format) into a temporary directory
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(filePath, tempDir);

            // Parse the main content file (usually content.opf) to retrieve book metadata and structure
            string contentOpfPath = FindContentOpfPath(tempDir);
            var epubFile = ParseOpfFile(contentOpfPath, tempDir);

            // Clean up temporary directory
            Directory.Delete(tempDir, true);

            return epubFile;
        }

        /// <summary>
        /// Finds the path to the content.opf file inside the extracted EPUB directory.
        /// </summary>
        /// <param name="tempDir">The temporary directory where the EPUB file is extracted.</param>
        /// <returns>The path to the content.opf file.</returns>
        /// <exception cref="Exception">Thrown if the EPUB file structure is invalid or if the OPF file is missing.</exception>
        private string FindContentOpfPath(string tempDir)
        {
            // Look for the OPF file inside the extracted EPUB directory
            string containerPath = Path.Combine(tempDir, "META-INF", "container.xml");
            if (!File.Exists(containerPath))
                throw new Exception("Invalid EPUB file structure.");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(containerPath);
            XmlNode? rootfileNode = xmlDoc.SelectSingleNode("//rootfile[@media-type='application/oebps-package+xml']")
                             ?? throw new Exception("Unable to locate the main content file.");

            // Explicitly check for null attributes to prevent null dereferencing
            XmlAttribute? fullPathAttr = rootfileNode.Attributes?["full-path"];
            if (fullPathAttr == null)
                throw new Exception("Full path attribute is missing in the rootfile node.");

            return Path.Combine(tempDir, fullPathAttr.Value);
        }

        /// <summary>
        /// Parses the OPF file to extract metadata and chapters from the EPUB file.
        /// </summary>
        /// <param name="opfFilePath">The path to the OPF file.</param>
        /// <param name="tempDir">The temporary directory where the EPUB content is extracted.</param>
        /// <returns>An <see cref="EpubFile"/> object containing the metadata and chapters of the EPUB.</returns>
        private EpubFile ParseOpfFile(string opfFilePath, string tempDir)
        {
            // Read and parse the content.opf file to build the EpubFile structure
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(opfFilePath);

            var epubFile = new EpubFile();

            // Parse metadata
            XmlNode? metadataNode = xmlDoc.SelectSingleNode("//metadata");
            if (metadataNode != null)
            {
                epubFile.Metadata = new EpubMetadata
                {
                    Title = metadataNode.SelectSingleNode("dc:title")?.InnerText ?? "Unknown Title",
                    Author = metadataNode.SelectSingleNode("dc:creator")?.InnerText ?? "Unknown Author",
                    Publisher = metadataNode.SelectSingleNode("dc:publisher")?.InnerText ?? string.Empty,
                    PublicationDate = DateTime.TryParse(metadataNode.SelectSingleNode("dc:date")?.InnerText, out var date) ? date : (DateTime?)null,
                    Language = metadataNode.SelectSingleNode("dc:language")?.InnerText ?? string.Empty,
                    Identifier = metadataNode.SelectSingleNode("dc:identifier")?.InnerText ?? string.Empty,
                    Description = metadataNode.SelectSingleNode("dc:description")?.InnerText ?? string.Empty
                };
            }

            // Parse manifest and spine to build the chapter list
            XmlNodeList? manifest = xmlDoc.SelectNodes("//manifest/item");
            XmlNodeList? spine = xmlDoc.SelectNodes("//spine/itemref");

            // Create a map from the manifest for id to href
            var idToHrefMap = new Dictionary<string, string>();
            foreach (XmlNode item in manifest != null ? manifest.Cast<XmlNode>() : Enumerable.Empty<XmlNode>())
            {
                // Check and retrieve 'id' and 'href' attributes safely
                XmlAttribute? idAttr = item.Attributes?["id"];
                XmlAttribute? hrefAttr = item.Attributes?["href"];
                if (idAttr != null && hrefAttr != null)
                {
                    idToHrefMap[idAttr.Value] = hrefAttr.Value;
                }
            }

            // Extract chapters based on spine and manifest
            foreach (XmlNode item in spine != null ? spine.Cast<XmlNode>() : Enumerable.Empty<XmlNode>())
            {
                string? idref = item.Attributes?["idref"]?.Value;

                // Check if idref is not null and map it to href using the idToHrefMap
                if (idref != null && idToHrefMap.TryGetValue(idref, out string href))
                {
                    // Construct the full path of the chapter file
                    string chapterPath = Path.Combine(Path.GetDirectoryName(opfFilePath) ?? string.Empty, href);

                    // Ensure the chapter file exists before reading
                    if (File.Exists(chapterPath))
                    {
                        // Read the content of the chapter file
                        string content = File.ReadAllText(chapterPath);

                        // Safely add the chapter, handling nullable values appropriately
                        epubFile.Chapters.Add(new EpubChapter
                        {
                            Title = Path.GetFileNameWithoutExtension(href) ?? "Untitled", // Provide a default title if null
                            Content = content,
                            FilePath = href
                        });
                    }
                }
            }

            return epubFile;
        }
    }
}