using System;
using System.Text.RegularExpressions;

namespace MauiApp1.Services
{
    public static class MarkdownRenderer
    {
        public static string ToHtml(string? markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
            try
            {
                return Markdig.Markdown.ToHtml(markdown);
            }
            catch
            {
                return System.Net.WebUtility.HtmlEncode(markdown ?? string.Empty);
            }
        }

        public static string RenderPreview(string? markdown, int maxRawChars = 300)
        {
            var raw = markdown ?? string.Empty;
            if (raw.Length > maxRawChars)
            {
                raw = raw.Substring(0, maxRawChars) + "...";
            }
            var html = ToHtml(raw);
            return SanitizeHtml(html);
        }

        // Basic sanitizer: remove scripts, on* attributes and javascript: links
        public static string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Use Ganss.XSS HtmlSanitizer if available
            try
            {
                var sanitizer = new Ganss.XSS.HtmlSanitizer();
                // keep common formatting tags
                sanitizer.AllowedTags.Add("h1");
                sanitizer.AllowedTags.Add("h2");
                sanitizer.AllowedTags.Add("h3");
                sanitizer.AllowedTags.Add("pre");
                sanitizer.AllowedTags.Add("code");
                sanitizer.AllowedTags.Add("img");
                // allow href/src but sanitize javascript: automatically
                return sanitizer.Sanitize(html);
            }
            catch
            {
                // fallback to previous basic regex cleaning
                html = Regex.Replace(html, @"<script[\s\S]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\son\w+\s*=\s*(?:'[^']*'|""[^""]*""|[^>\s]+)", "", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"(href|src)\s*=\s*""javascript:[^""]*""", "", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"(href|src)\s*=\s*'javascript:[^']*'", "", RegexOptions.IgnoreCase);
                return html;
            }
        }
    }
}
