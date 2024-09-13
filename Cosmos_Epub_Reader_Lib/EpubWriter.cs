using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Cosmos_Epub_Reader_Lib
{
    /// <summary>
    /// Provides functionality to write and save EPUB files, including metadata, chapters, and necessary EPUB structure files.
    /// </summary>
    public class EpubWriter
    {
        /// <summary>
        /// Saves an <see cref="EpubFile"/> to the specified output path in EPUB format.
        /// </summary>
        /// <param name="epub">The <see cref="EpubFile"/> object containing the content to be saved.</param>
        /// <param name="outputPath">The path where the EPUB file will be saved.</param>
        /// <exception cref="Exception">Thrown if an error occurs during the save process.</exception>
        public void SaveEpub(EpubFile epub, string outputPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create the necessary directories for EPUB structure
                string oebpsDir = Path.Combine(tempDir, "OEBPS");
                Directory.CreateDirectory(oebpsDir);

                // Write the OPF file and other necessary files to tempDir
                WriteOpfFile(epub, oebpsDir);

                // Write chapter files to the OEBPS directory
                WriteChapters(epub, oebpsDir);

                // Create META-INF directory and write container.xml
                string metaInfDir = Path.Combine(tempDir, "META-INF");
                Directory.CreateDirectory(metaInfDir);
                WriteContainerXml(metaInfDir);

                // Package everything into an EPUB file (ZIP format)
                ZipFile.CreateFromDirectory(tempDir, outputPath);
            }
            finally
            {
                // Clean up temporary files
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Writes the OPF file with metadata, manifest, and spine details for the EPUB.
        /// </summary>
        /// <param name="epub">The <see cref="EpubFile"/> object containing the content to be saved.</param>
        /// <param name="directoryPath">The directory path where the OPF file will be saved.</param>
        private void WriteOpfFile(EpubFile epub, string directoryPath)
        {
            // Create and write the content.opf XML file using epub data
            var opfPath = Path.Combine(directoryPath, "content.opf");
            using (var writer = XmlWriter.Create(opfPath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("package");
                writer.WriteAttributeString("xmlns", "http://www.idpf.org/2007/opf");
                writer.WriteAttributeString("version", "2.0");

                // Write metadata
                writer.WriteStartElement("metadata");
                writer.WriteAttributeString("xmlns:dc", "http://purl.org/dc/elements/1.1/");
                writer.WriteElementString("dc:title", epub.Metadata.Title);
                writer.WriteElementString("dc:creator", epub.Metadata.Author);
                writer.WriteElementString("dc:publisher", epub.Metadata.Publisher);
                writer.WriteElementString("dc:date", epub.Metadata.PublicationDate?.ToString("yyyy-MM-dd") ?? string.Empty);
                writer.WriteElementString("dc:language", epub.Metadata.Language);
                writer.WriteElementString("dc:identifier", epub.Metadata.Identifier);
                writer.WriteElementString("dc:description", epub.Metadata.Description);
                writer.WriteEndElement(); // metadata

                // Write manifest
                writer.WriteStartElement("manifest");
                foreach (var chapter in epub.Chapters)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("id", Path.GetFileNameWithoutExtension(chapter.FilePath));
                    writer.WriteAttributeString("href", chapter.FilePath);
                    writer.WriteAttributeString("media-type", "application/xhtml+xml");
                    writer.WriteEndElement(); // item
                }
                writer.WriteEndElement(); // manifest

                // Write spine
                writer.WriteStartElement("spine");
                foreach (var chapter in epub.Chapters)
                {
                    writer.WriteStartElement("itemref");
                    writer.WriteAttributeString("idref", Path.GetFileNameWithoutExtension(chapter.FilePath));
                    writer.WriteEndElement(); // itemref
                }
                writer.WriteEndElement(); // spine

                writer.WriteEndElement(); // package
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Writes the chapter files to the OEBPS directory within the EPUB structure.
        /// </summary>
        /// <param name="epub">The <see cref="EpubFile"/> object containing the chapters to be saved.</param>
        /// <param name="oebpsDir">The directory where the chapter files will be saved.</param>
        private void WriteChapters(EpubFile epub, string oebpsDir)
        {
            foreach (var chapter in epub.Chapters)
            {
                // Ensure chapter.FilePath is not null by providing a fallback value
                string chapterFilePath = Path.Combine(oebpsDir, chapter.FilePath ?? "default-chapter.xhtml");
                Directory.CreateDirectory(Path.GetDirectoryName(chapterFilePath) ?? string.Empty); // Ensure directories exist

                // Write chapter content
                File.WriteAllText(chapterFilePath, chapter.Content);
            }
        }

        /// <summary>
        /// Writes the container.xml file inside the META-INF directory, defining the path to the OPF file.
        /// </summary>
        /// <param name="metaInfDir">The path to the META-INF directory where the container.xml will be saved.</param>
        private void WriteContainerXml(string metaInfDir)
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