using ServiceNowCLI.Core.Aikido.Models;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace ServiceNowCLI.Core.Aikido
{
    internal static class ReportGenerator
    {
        public static void GeneratePdfReport(string repoName, List<Issue> issues, string filePath)
        {
            var document = GeneratePdfReport(repoName, issues);
            document.GeneratePdf(filePath);
            Console.WriteLine($"PDF report generated: {filePath}");
        }

        public static void GeneratePdfReport(string repoName, List<Issue> issues, Stream stream)
        {
            var document = GeneratePdfReport(repoName, issues);
            document.GeneratePdf(stream);
            Console.WriteLine($"PDF report generated in stream.");
        }

        private static Document GeneratePdfReport(string repoName, List<Issue> issues)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(842, 595)); // A4 landscape
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Inter"));

                    // Header Section
                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Security Audit Report for {repoName}")
                            .FontSize(16)
                            .SemiBold()
                            .FontColor(Colors.Black)
                            .AlignCenter();

                        header.Item().Text($"Created on {DateTime.Now:dd MMMM yyyy @ HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium)
                            .AlignCenter();
                    });

                    // Content Section (Table)
                    page.Content().PaddingVertical(7, Unit.Millimetre).Column(content =>
                    {
                        // Table Header
                        content.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(1, Unit.Millimetre).Row(row =>
                        {
                            row.RelativeItem(1).Text("Type").Bold();
                            row.RelativeItem(2).Text("Issue").Bold();
                            row.RelativeItem(1).Text("Severity").Bold();
                            row.RelativeItem(2).Text("Package").Bold();
                            row.RelativeItem(4).Text("Affected File").Bold();
                            row.RelativeItem(1).Text("Detection Date").Bold();
                        });

                        // Table Rows
                        foreach (var issue in issues)
                        {
                            content.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(1, Unit.Millimetre).Row(row =>
                            {
                                row.RelativeItem(1).Text(issue.type);
                                row.RelativeItem(2).Text(issue.rule);
                                CreateSeverityCell(row, issue.severity);
                                row.RelativeItem(2).Text(issue.affected_package ?? "");
                                row.RelativeItem(4).Text(issue.affected_file ?? "");
                                row.RelativeItem(1).Text(issue.FirstDetectedAtDate.ToString("dd MMM yyyy"));
                            });
                        }
                    });
                });
            });
        }

        private static void CreateSeverityCell(RowDescriptor row, string severity)
        {
            switch (severity.ToLower())
            {
                case "critical":
                    row.RelativeItem(1).AlignMiddle()
                        .Text($"🛇 {severity}")
                        .FontSize(12).FontColor(Colors.Red.Medium);
                    break;

                case "high":
                    row.RelativeItem(1).AlignMiddle()
                        .Text($"⚠ {severity}")
                        .FontSize(12).FontColor(Colors.Orange.Medium);
                    break;
                case "medium":
                    row.RelativeItem(1).AlignMiddle()
                        .Text($"{severity}")
                        .FontSize(12).FontColor(Colors.Blue.Medium);
                    break;
                case "low":
                    row.RelativeItem(1).AlignMiddle()
                        .Text($"{severity}")
                        .FontSize(12).FontColor(Colors.Green.Medium);
                    break;
                default:
                    row.RelativeItem(1).AlignMiddle()
                        .Text($"{severity}")
                        .FontSize(12).FontColor(Colors.Black);
                    break;
            }
        }
    }
}
