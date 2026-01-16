using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using QColors = QuestPDF.Helpers.Colors;

namespace MauiApp1.Services
{
    public class PdfExportService
    {
        public async Task<string> ExportEntriesToPdfAsync(List<JournalEntry> entries, string outputPath)
        {
            return await Task.Run(() =>
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(QColors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .PaddingBottom(10)
                            .Text("My Journal Entries")
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                if (entries == null || entries.Count == 0)
                                {
                                    column.Item().Text("No entries to export.").FontSize(12);
                                }
                                else
                                {
                                    foreach (var entry in entries.OrderByDescending(e => e.EntryDate))
                                    {
                                        column.Item().Element(container => RenderEntry(container, entry));
                                    }
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .DefaultTextStyle(x => x.FontSize(9).FontColor(QColors.Grey.Medium))
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                document.GeneratePdf(outputPath);
                return outputPath;
            });
        }

        private void RenderEntry(QuestPDF.Infrastructure.IContainer container, JournalEntry entry)
        {
            container
                .Border(1)
                .BorderColor(QColors.Grey.Lighten2)
                .Padding(15)
                .Column(column =>
                {
                    column.Item()
                        .PaddingBottom(8)
                        .Row(row =>
                        {
                            row.AutoItem()
                                .Text(entry.EntryDate.ToString("dddd, MMMM dd, yyyy"))
                                .FontSize(16)
                                .Bold()
                                .FontColor(QColors.Blue.Darken2);
                            
                            row.RelativeItem();
                            
                            if (!string.IsNullOrEmpty(entry.Category))
                            {
                                row.AutoItem()
                                    .PaddingLeft(10)
                                    .Background(QColors.Blue.Lighten4)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(8)
                                    .Text(entry.Category)
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(QColors.Blue.Darken3);
                            }
                        });

                    if (!string.IsNullOrEmpty(entry.PrimaryMood))
                    {
                        column.Item()
                            .PaddingTop(5)
                            .PaddingBottom(5)
                            .Row(row =>
                            {
                                row.AutoItem().Text("Mood: ").FontSize(11).FontColor(QColors.Grey.Darken1);
                                row.AutoItem()
                                    .Background(GetMoodColor(entry.PrimaryMood))
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(8)
                                    .Text(entry.PrimaryMood)
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(QColors.White);

                                if (!string.IsNullOrEmpty(entry.SecondaryMood1))
                                {
                                    row.AutoItem().PaddingLeft(5);
                                    row.AutoItem()
                                        .Background(GetMoodColor(entry.SecondaryMood1))
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(8)
                                        .Text(entry.SecondaryMood1)
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(QColors.White);
                                }

                                if (!string.IsNullOrEmpty(entry.SecondaryMood2))
                                {
                                    row.AutoItem().PaddingLeft(5);
                                    row.AutoItem()
                                        .Background(GetMoodColor(entry.SecondaryMood2))
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(8)
                                        .Text(entry.SecondaryMood2)
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(QColors.White);
                                }
                            });
                    }

                    if (!string.IsNullOrEmpty(entry.Tags))
                    {
                        var tags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();

                        if (tags.Count > 0)
                        {
                            column.Item()
                                .PaddingTop(5)
                                .PaddingBottom(5)
                                .Row(row =>
                                {
                                    row.AutoItem().Text("Tags: ").FontSize(11).FontColor(QColors.Grey.Darken1);
                                    foreach (var tag in tags)
                                    {
                                        row.AutoItem().PaddingLeft(3);
                                        row.AutoItem()
                                            .Background(QColors.Grey.Lighten3)
                                            .PaddingVertical(3)
                                            .PaddingHorizontal(6)
                                            .Text(tag)
                                            .FontSize(9)
                                            .FontColor(QColors.Grey.Darken2);
                                    }
                                });
                        }
                    }

                    column.Item()
                        .PaddingTop(10)
                        .Element(container => RenderMarkdownContent(container, entry.Content ?? ""));

                    column.Item()
                        .PaddingTop(10)
                        .BorderTop(1)
                        .BorderColor(QColors.Grey.Lighten3)
                        .PaddingTop(8)
                        .Row(row =>
                        {
                            row.AutoItem()
                                .Text($"Created: {entry.CreatedAt:yyyy-MM-dd HH:mm}")
                                .FontSize(9)
                                .FontColor(QColors.Grey.Medium);

                            if (entry.UpdatedAt != entry.CreatedAt)
                            {
                                row.RelativeItem();
                                row.AutoItem()
                                    .Text($"Updated: {entry.UpdatedAt:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(QColors.Grey.Medium);
                            }
                        });
                });
        }

        private void RenderMarkdownContent(QuestPDF.Infrastructure.IContainer container, string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                container.Text("");
                return;
            }

            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            container.Column(column =>
            {
                column.Spacing(6);

                var i = 0;
                while (i < lines.Length)
                {
                    var line = lines[i];
                    
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        column.Item().Height(6); 
                        i++;
                        continue;
                    }

                    var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                    if (headerMatch.Success)
                    {
                        var level = headerMatch.Groups[1].Value.Length;
                        var text = headerMatch.Groups[2].Value.Trim();
                        var fontSize = level switch
                        {
                            1 => 18f,
                            2 => 16f,
                            3 => 14f,
                            4 => 13f,
                            5 => 12f,
                            6 => 11f,
                            _ => 11f
                        };
                        column.Item()
                            .PaddingBottom(4)
                            .Text(text)
                            .FontSize(fontSize)
                            .Bold()
                            .FontColor(QColors.Blue.Darken2);
                        i++;
                        continue;
                    }

                    if (Regex.IsMatch(line, @"^---+$"))
                    {
                        column.Item()
                            .PaddingVertical(8)
                            .BorderBottom(1)
                            .BorderColor(QColors.Grey.Lighten2);
                        i++;
                        continue;
                    }

                    if (Regex.IsMatch(line, @"^[\-\*\+]\s+"))
                    {
                        var listItems = new List<string>();
                        
                        while (i < lines.Length && Regex.IsMatch(lines[i], @"^[\-\*\+]\s+"))
                        {
                            var listText = Regex.Replace(lines[i], @"^[\-\*\+]\s+", "");
                            listItems.Add(listText);
                            i++;
                        }
                        
                        column.Item()
                            .PaddingLeft(20)
                            .Column(listColumn =>
                            {
                                listColumn.Spacing(4);
                                foreach (var listItem in listItems)
                                {
                                    listColumn.Item()
                                        .Text(text =>
                                        {
                                            text.DefaultTextStyle(style => style.FontSize(11).LineHeight(1.6f));
                                            text.Span("• ");
                                            var processed = ProcessInlineMarkdown(listItem);
                                            foreach (var segment in processed)
                                            {
                                                var span = text.Span(segment.Text);
                                                if (segment.IsBold) span.Bold();
                                                if (segment.IsItalic) span.Italic();
                                                if (segment.IsCode)
                                                {
                                                    span.FontFamily("Courier")
                                                        .BackgroundColor(QColors.Grey.Lighten4)
                                                        .FontSize(10);
                                                }
                                            }
                                        });
                                }
                            });
                        continue;
                    }

                    column.Item()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(style => style.FontSize(11).LineHeight(1.6f));
                            RenderInlineMarkdownText(text, line);
                        });
                    i++;
                }
            });
        }

        private void RenderInlineMarkdownText(QuestPDF.Fluent.TextDescriptor text, string content)
        {
            var processed = ProcessInlineMarkdown(content);
            
            foreach (var segment in processed)
            {
                var span = text.Span(segment.Text);
                
                if (segment.IsBold)
                    span.Bold();
                if (segment.IsItalic)
                    span.Italic();
                if (segment.IsCode)
                {
                    span.FontFamily("Courier")
                        .BackgroundColor(QColors.Grey.Lighten4)
                        .FontSize(10);
                }
            }
        }

        private List<TextSegment> ProcessInlineMarkdown(string content)
        {
            var segments = new List<TextSegment>();
            if (string.IsNullOrEmpty(content))
                return segments;

            var i = 0;
            var currentText = new System.Text.StringBuilder();
            var isBold = false;
            var isItalic = false;
            var isCode = false;

            while (i < content.Length)
            {
                if (content[i] == '`' && (i == 0 || content[i - 1] != '\\'))
                {
                    if (currentText.Length > 0)
                    {
                        segments.Add(new TextSegment
                        {
                            Text = currentText.ToString(),
                            IsBold = isBold,
                            IsItalic = isItalic,
                            IsCode = isCode
                        });
                        currentText.Clear();
                    }
                    isCode = !isCode;
                    i++;
                    continue;
                }

                if (isCode)
                {
                    currentText.Append(content[i]);
                    i++;
                    continue;
                }

                if (i < content.Length - 1 && 
                    ((content[i] == '*' && content[i + 1] == '*') || 
                     (content[i] == '_' && content[i + 1] == '_')))
                {
                    if (currentText.Length > 0)
                    {
                        segments.Add(new TextSegment
                        {
                            Text = currentText.ToString(),
                            IsBold = isBold,
                            IsItalic = isItalic,
                            IsCode = isCode
                        });
                        currentText.Clear();
                    }
                    isBold = !isBold;
                    i += 2;
                    continue;
                }

                if ((content[i] == '*' || content[i] == '_') && 
                    (i == content.Length - 1 || content[i + 1] != content[i]))
                {
                    if (currentText.Length > 0)
                    {
                        segments.Add(new TextSegment
                        {
                            Text = currentText.ToString(),
                            IsBold = isBold,
                            IsItalic = isItalic,
                            IsCode = isCode
                        });
                        currentText.Clear();
                    }
                    isItalic = !isItalic;
                    i++;
                    continue;
                }

                currentText.Append(content[i]);
                i++;
            }

            if (currentText.Length > 0)
            {
                segments.Add(new TextSegment
                {
                    Text = currentText.ToString(),
                    IsBold = isBold,
                    IsItalic = isItalic,
                    IsCode = isCode
                });
            }

            return segments;
        }

        private class TextSegment
        {
            public string Text { get; set; } = "";
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsCode { get; set; }
        }


        private string GetMoodColor(string mood)
        {
            var category = MoodCategories.GetMoodCategory(mood);
            return category switch
            {
                "Positive" => "#28a745",
                "Neutral" => "#6c757d",
                "Negative" => "#dc3545",
                _ => "#667eea"
            };
        }
    }
}

