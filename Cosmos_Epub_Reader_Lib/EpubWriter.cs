using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Cosmos_Epub_Reader_Lib
{
    public class EpubWriter
    {
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

        private void WriteOpfFile(EpubFile epub, string directoryPath)
        {
            // Create and write the content.opf XML file using epub data
            // This includes writing metadata, manifest, and spine
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