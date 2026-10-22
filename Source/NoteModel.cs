// Zen Notes - Masaüstü Kenara Katlanabilir Akıllı Notlar
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZenNotes
{
    public class NoteData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ColorTheme { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsPinned { get; set; }
        public bool IsRolledUp { get; set; }
        public DateTime ModifiedDate { get; set; }

        public NoteData()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "Yeni Not";
            Content = "";
            ColorTheme = "Obsidian";
            Width = 320;
            Height = 340;
            Left = 200;
            Top = 150;
            IsPinned = true;
            IsRolledUp = false;
            ModifiedDate = DateTime.Now;
        }
    }

    public static class NotesStorage
    {
        private static string GetStoragePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "ZenSuite", "ZenNotes");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "notes.json");
        }

        public static List<NoteData> LoadNotes()
        {
            var list = new List<NoteData>();
            string file = GetStoragePath();

            if (!File.Exists(file))
            {
                // Default Welcome Note
                var welcome = new NoteData
                {
                    Title = "☯️ Hoş Geldiniz - Zen Notes",
                    Content = "Zen Notes masaüstü not alma aracına hoş geldiniz!\n\n" +
                              "✨ Özellikler:\n" +
                              "- Notu taşımak için başlığı sürükleyin.\n" +
                              "- '▲' butonuna veya başlığa çift tıklayarak notu kenara katlayın.\n" +
                              "- 📌 butonu ile en üstte sabitleyin.\n" +
                              "- 🎨 Renk paleti ile not rengini özelleştirin.\n" +
                              "- Yeni not için ➕ butonuna veya Ctrl+Shift+N kısayoluna basın.\n\n" +
                              "Serkan ŞENTÜRK tarafından özenle geliştirildi.",
                    ColorTheme = "Obsidian",
                    Left = 250,
                    Top = 180,
                    Width = 340,
                    Height = 360,
                    IsPinned = true
                };
                list.Add(welcome);
                SaveNotes(list);
                return list;
            }

            try
            {
                string json = File.ReadAllText(file, Encoding.UTF8);
                // Simple fast JSON deserialization without Json.NET
                list = ParseNotesJson(json);
            }
            catch
            {
                // Fallback
            }

            if (list.Count == 0)
            {
                list.Add(new NoteData());
            }

            return list;
        }

        public static void SaveNotes(List<NoteData> list)
        {
            try
            {
                string file = GetStoragePath();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < list.Count; i++)
                {
                    var n = list[i];
                    sb.AppendLine("  {");
                    sb.AppendFormat("    \"Id\": \"{0}\",\n", EscapeJson(n.Id));
                    sb.AppendFormat("    \"Title\": \"{0}\",\n", EscapeJson(n.Title));
                    sb.AppendFormat("    \"Content\": \"{0}\",\n", EscapeJson(n.Content));
                    sb.AppendFormat("    \"ColorTheme\": \"{0}\",\n", EscapeJson(n.ColorTheme));
                    sb.AppendFormat("    \"Left\": {0},\n", n.Left);
                    sb.AppendFormat("    \"Top\": {0},\n", n.Top);
                    sb.AppendFormat("    \"Width\": {0},\n", n.Width);
                    sb.AppendFormat("    \"Height\": {0},\n", n.Height);
                    sb.AppendFormat("    \"IsPinned\": {0},\n", n.IsPinned ? "true" : "false");
                    sb.AppendFormat("    \"IsRolledUp\": {0}\n", n.IsRolledUp ? "true" : "false");
                    sb.Append("  }");
                    if (i < list.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "")
                    .Replace("\n", "\\n");
        }

        private static List<NoteData> ParseNotesJson(string json)
        {
            var list = new List<NoteData>();
            if (string.IsNullOrEmpty(json)) return list;

            int idx = 0;
            while ((idx = json.IndexOf('{', idx)) != -1)
            {
                int end = json.IndexOf('}', idx);
                if (end == -1) break;
                string block = json.Substring(idx, end - idx + 1);
                var note = new NoteData();

                note.Id = ExtractJsonString(block, "Id") ?? Guid.NewGuid().ToString("N");
                note.Title = ExtractJsonString(block, "Title") ?? "Not";
                note.Content = ExtractJsonString(block, "Content") ?? "";
                note.ColorTheme = ExtractJsonString(block, "ColorTheme") ?? "Obsidian";
                note.Left = ExtractJsonDouble(block, "Left", 200);
                note.Top = ExtractJsonDouble(block, "Top", 150);
                note.Width = ExtractJsonDouble(block, "Width", 320);
                note.Height = ExtractJsonDouble(block, "Height", 340);
                note.IsPinned = ExtractJsonBool(block, "IsPinned", true);
                note.IsRolledUp = ExtractJsonBool(block, "IsRolledUp", false);

                list.Add(note);
                idx = end + 1;
            }

            return list;
        }

        private static string ExtractJsonString(string block, string key)
        {
            string token = "\"" + key + "\": \"";
            int p = block.IndexOf(token);
            if (p == -1) return null;
            p += token.Length;
            StringBuilder sb = new StringBuilder();
            bool escape = false;
            for (int i = p; i < block.Length; i++)
            {
                char c = block[i];
                if (escape)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') { }
                    else sb.Append(c);
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '\"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static double ExtractJsonDouble(string block, string key, double def)
        {
            string token = "\"" + key + "\":";
            int p = block.IndexOf(token);
            if (p == -1) return def;
            p += token.Length;
            int comma = block.IndexOfAny(new char[] { ',', '\n', '\r', '}' }, p);
            if (comma == -1) comma = block.Length;
            string val = block.Substring(p, comma - p).Trim();
            double d;
            if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
                return d;
            return def;
        }

        private static bool ExtractJsonBool(string block, string key, bool def)
        {
            string token = "\"" + key + "\":";
            int p = block.IndexOf(token);
            if (p == -1) return def;
            p += token.Length;
            int comma = block.IndexOfAny(new char[] { ',', '\n', '\r', '}' }, p);
            if (comma == -1) comma = block.Length;
            string val = block.Substring(p, comma - p).Trim().ToLowerInvariant();
            if (val.StartsWith("true")) return true;
            if (val.StartsWith("false")) return false;
            return def;
        }
    }
}
