using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace Cosmos_Epub_Reader_Lib
{
    public class EpubFile
    {
        public EpubMetadata Metadata { get; set; } = new EpubMetadata();
        public List<EpubChapter> Chapters { get; set; } = new List<EpubChapter>();

        // Method to add a new chapter to the list
        public void AddChapter(EpubChapter chapter)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            Chapters.Add(chapter);
        }

        // Method to find a chapter by its title
        public EpubChapter? FindChapterByTitle(string title)
        {
            return Chapters.FirstOrDefault(c => c.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }

        // Method to remove a chapter by its title
        public bool RemoveChapterByTitle(string title)
        {
            var chapter = FindChapterByTitle(title);
            if (chapter != null)
            {
                Chapters.Remove(chapter);
                return true;
            }
            return false;
        }

        // Validation method to ensure Metadata is properly populated
        public bool ValidateMetadata(out string validationMessage)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Metadata.Title))
                errors.Add("Title is missing.");

            if (string.IsNullOrWhiteSpace(Metadata.Author))
                errors.Add("Author is missing.");

            if (errors.Any())
            {
                validationMessage = string.Join(Environment.NewLine, errors);
                return false;
            }

            validationMessage = "Metadata is valid.";
            return true;
        }

        // Load EPUB content from a file using System.IO.Compression and XML parsing
        public static EpubFile LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified EPUB file does not exist.", filePath);

            var epubFile = new EpubFile();
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Extract the EPUB file (ZIP format) into a temporary directory
                ZipFile.ExtractToDirectory(filePath, tempDir);

                // Locate the OPF file path through META-INF/container.xml
                string containerPath = Path.Combine(tempDir, "META-INF", "container.xml");
                if (!File.Exists(containerPath))
                    throw new Exception("Invalid EPUB file structure: Missing container.xml.");

                XmlDocument containerDoc = new XmlDocument();
                containerDoc.Load(containerPath);
                XmlNode? rootfileNode = containerDoc.SelectSingleNode("//rootfile[@media-type='application/oebps-package+xml']");
                if (rootfileNode == null)
                    throw new Exception("Unable to locate the main content file.");

                string opfPath = Path.Combine(tempDir, rootfileNode.Attributes?["full-path"]?.Value ?? string.Empty);
                if (!File.Exists(opfPath))
                    throw new Exception("The content.opf file is missing or inaccessible.");

                // Parse the OPF file to extract metadata and chapters
                epubFile.ParseOpfFile(opfPath, tempDir);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load EPUB file: {ex.Message}", ex);
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }

            return epubFile;
        }

        // Parses the OPF file to extract metadata and chapter details
        private void ParseOpfFile(string opfFilePath, string tempDir)
        {
            XmlDocument opfDoc = new XmlDocument();
            opfDoc.Load(opfFilePath);

            // Extract metadata
            XmlNode? metadataNode = opfDoc.SelectSingleNode("//metadata");
            if (metadataNode != null)
            {
                Metadata.Title = metadataNode.SelectSingleNode("dc:title")?.InnerText ?? "Unknown Title";
                Metadata.Author = metadataNode.SelectSingleNode("dc:creator")?.InnerText ?? "Unknown Author";
            }

            // Extract chapters based on manifest and spine
            XmlNodeList? manifest = opfDoc.SelectNodes("//manifest/item");
            XmlNodeList? spine = opfDoc.SelectNodes("//spine/itemref");

            // Convert manifest to IEnumerable<XmlNode> safely
            var manifestNodes = manifest != null ? manifest.Cast<XmlNode>() : Enumerable.Empty<XmlNode>();
            var spineNodes = spine != null ? spine.Cast<XmlNode>() : Enumerable.Empty<XmlNode>();

            // Map manifest items to spine order
            var idToHrefMap = new Dictionary<string, string>();
            foreach (XmlNode item in manifestNodes)
            {
                // Safely check and retrieve the 'id' and 'href' attributes
                XmlAttribute? idAttribute = item?.Attributes?["id"];
                XmlAttribute? hrefAttribute = item?.Attributes?["href"];

                // Check if both attributes are not null before accessing their values
                if (idAttribute != null && hrefAttribute != null)
                {
                    idToHrefMap[idAttribute.Value] = hrefAttribute.Value;
                }
            }


            // Build the chapters based on spine references
            foreach (XmlNode item in spineNodes)
            {
                // Retrieve the idref attribute safely
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
                        Chapters.Add(new EpubChapter
                        {
                            Title = Path.GetFileNameWithoutExtension(href) ?? "Untitled", // Provide a default title if null
                            Content = content,
                            FilePath = href
                        });
                    }
                }
            }
        }

        // Save EPUB content to a file using System.IO.Compression
        public void SaveToFile(string filePath)
        {
            // Perform basic validation before saving
            if (!ValidateMetadata(out string validationMessage))
                throw new InvalidOperationException($"EPUB file validation failed: {validationMessage}");

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);
                string opfDir = Path.Combine(tempDir, "OEBPS");
                Directory.CreateDirectory(opfDir);

                // Create content.opf with metadata and spine information
                string opfFilePath = Path.Combine(opfDir, "content.opf");
                CreateOpfFile(opfFilePath);

                // Create META-INF/container.xml
                string metaInfDir = Path.Combine(tempDir, "META-INF");
                Directory.CreateDirectory(metaInfDir);
                CreateContainerXml(metaInfDir);

                // Write chapter files
                foreach (var chapter in Chapters)
                {
                    // Ensure that chapter.FilePath is not null by providing a fallback value or handling the null case
                    string chapterPath = Path.Combine(Path.GetDirectoryName(opfFilePath) ?? string.Empty, chapter.FilePath ?? string.Empty);

                    // Write the chapter content to the determined path
                    File.WriteAllText(chapterPath, chapter.Content);
                }


                // Create the EPUB file as a ZIP archive with .epub extension
                ZipFile.CreateFromDirectory(tempDir, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save EPUB file: {ex.Message}", ex);
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        // Creates the OPF file with metadata and spine information
        private void CreateOpfFile(string opfFilePath)
        {
            using (var writer = XmlWriter.Create(opfFilePath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("package");
                writer.WriteAttributeString("xmlns", "http://www.idpf.org/2007/opf");
                writer.WriteAttributeString("version", "2.0");

                // Write metadata
                writer.WriteStartElement("metadata");
                writer.WriteElementString("dc:title", Metadata.Title);
                writer.WriteElementString("dc:creator", Metadata.Author);
                writer.WriteEndElement(); // metadata

                // Write manifest
                writer.WriteStartElement("manifest");
                foreach (var chapter in Chapters)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("id", Path.GetFileNameWithoutExtension(chapter.FilePath));
                    writer.WriteAttributeString("href", chapter.FilePath);
                    writer.WriteAttributeString("media-type", "application/xhtml+xml");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement(); // manifest

                // Write spine
                writer.WriteStartElement("spine");
                foreach (var chapter in Chapters)
                {
                    writer.WriteStartElement("itemref");
                    writer.WriteAttributeString("idref", Path.GetFileNameWithoutExtension(chapter.FilePath));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement(); // spine

                writer.WriteEndElement(); // package
                writer.WriteEndDocument();
            }
        }

        // Creates the container.xml file inside META-INF
        private void CreateContainerXml(string metaInfDir)
        {
            string containerPath = Path.Combine(metaInfDir, "container.xml");
            using (var writer = XmlWriter.Create(containerPath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("container");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("xmlns", "urn:oasis:names:tc:opendocument:xmlns:container");
                writer.WriteStartElement("rootfiles");
                writer.WriteStartElement("rootfile");
                writer.WriteAttributeString("full-path", "OEBPS/content.opf");
                writer.WriteAttributeString("media-type", "application/oebps-package+xml");
                writer.WriteEndElement(); // rootfile
                writer.WriteEndElement(); // rootfiles
                writer.WriteEndElement(); // container
                writer.WriteEndDocument();
            }
        }
    }
}