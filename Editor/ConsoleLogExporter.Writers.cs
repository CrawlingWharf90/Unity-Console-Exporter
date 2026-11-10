using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;


namespace CrawlingWharf90.ConsoleExporter
{
public static partial class ConsoleLogExporter
{
    // ---------------------------------------------------------
    // DATA MODEL
    // ---------------------------------------------------------

    [Serializable]
    public class ConsoleEntry
    {
        public string timestamp;
        public string type;
        public string message;
        public string file;
        public int line;
    }

    [Serializable]
    public class ExportDocument
    {
        public string exportedAt;
        public string filter;
        public List<ConsoleEntry> entries;
    }

    private static ExportDocument CreateDocument(
        List<ConsoleEntry> entries,
        ExportFilter filter)
    {
        return new ExportDocument
        {
            exportedAt = DateTime.Now.ToString("o"),
            filter = filter.ToString(),
            entries = entries
        };
    }

    // ---------------------------------------------------------
    // TXT
    // ---------------------------------------------------------

    private static void ExportTxt(
        string path,
        List<ConsoleEntry> entries)
    {
        using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);

        writer.WriteLine("UNITY CONSOLE EXPORT");
        writer.WriteLine($"Exported: {DateTime.Now}");
        writer.WriteLine($"Entries: {entries.Count}");
        writer.WriteLine(new string('=', 70));

        foreach (ConsoleEntry entry in entries)
        {
            writer.WriteLine(
                $"[{entry.timestamp}] [{entry.type}]"
            );

            writer.WriteLine(entry.message);

            if (!string.IsNullOrEmpty(entry.file))
                writer.WriteLine(
                    $"Source: {entry.file}:{entry.line}"
                );

            writer.WriteLine(new string('-', 70));
        }
    }

    // ---------------------------------------------------------
    // JSON
    // ---------------------------------------------------------

    private static void ExportJson(
        string path,
        List<ConsoleEntry> entries,
        ExportFilter filter)
    {
        ExportDocument document = CreateDocument(entries, filter);

        File.WriteAllText(
            path,
            JsonUtility.ToJson(document, true),
            Encoding.UTF8
        );
    }

    // ---------------------------------------------------------
    // CSV
    // ---------------------------------------------------------

    private static string CsvEscape(string value)
    {
        value ??= "";

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void ExportCsv(
        string path,
        List<ConsoleEntry> entries)
    {
        using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);

        writer.WriteLine(
            "Timestamp,Type,Message,File,Line"
        );

        foreach (ConsoleEntry entry in entries)
        {
            writer.WriteLine(string.Join(",",
                CsvEscape(entry.timestamp),
                CsvEscape(entry.type),
                CsvEscape(entry.message),
                CsvEscape(entry.file),
                entry.line.ToString()
            ));
        }
    }

    // ---------------------------------------------------------
    // XML
    // ---------------------------------------------------------

    private static void ExportXml(
        string path,
        List<ConsoleEntry> entries,
        ExportFilter filter)
    {
        ExportDocument document = CreateDocument(entries, filter);

        XmlSerializer serializer = new XmlSerializer(typeof(ExportDocument));

        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8
        };

        using XmlWriter writer = XmlWriter.Create(path, settings);

        serializer.Serialize(writer, document);
    }
}
}
