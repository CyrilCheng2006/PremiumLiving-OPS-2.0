using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// PdfSharp 6.x IFontResolver that reads TrueType fonts directly from
    /// the Windows Fonts folder (%WINDIR%\Fonts).  Supports the four
    /// faces (Regular, Bold, Italic, BoldItalic) for any family that ships
    /// as standard Windows fonts (Arial, Segoe UI, Calibri, …).
    ///
    /// Register once at application start (or lazily before first PDF export):
    ///   GlobalFontSettings.FontResolver = new WindowsFontResolver();
    /// </summary>
    public sealed class WindowsFontResolver : IFontResolver
    {
        // Static, registered once
        private static bool _registered = false;
        private static readonly object _lock = new object();

        public static void EnsureRegistered()
        {
            if (_registered) return;
            lock (_lock)
            {
                if (_registered) return;
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
                _registered = true;
            }
        }

        // ── IFontResolver ───────────────────────────────────────────────────

        public FontResolverInfo ResolveTypeface(
            string familyName, bool isBold, bool isItalic)
        {
            string key = BuildKey(familyName, isBold, isItalic);
            return new FontResolverInfo(key);
        }

        public byte[] GetFont(string faceName)
        {
            // faceName is what we returned as the key in ResolveTypeface
            string path = FindFontFile(faceName);
            if (path != null && File.Exists(path))
                return File.ReadAllBytes(path);

            // Absolute fallback — try arial.ttf directly
            string fallback = Path.Combine(WindowsFontsDir, "arial.ttf");
            if (File.Exists(fallback)) return File.ReadAllBytes(fallback);

            throw new FileNotFoundException(
                $"Font file not found for face '{faceName}'. "
              + "Ensure the font is installed in %WINDIR%\\Fonts.");
        }

        // ── Internals ────────────────────────────────────────────────────────

        private static readonly string WindowsFontsDir =
            Path.Combine(
                Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows",
                "Fonts");

        /// <summary>
        /// Maps (family, bold, italic) to a filename stem stored in Windows Fonts.
        /// Extend this dictionary if you need additional families.
        /// </summary>
        private static readonly Dictionary<string, string> FaceMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Arial
                { "Arial|false|false",  "arial.ttf"   },
                { "Arial|true|false",   "arialbd.ttf"  },
                { "Arial|false|true",   "ariali.ttf"   },
                { "Arial|true|true",    "arialbi.ttf"  },
                // Segoe UI
                { "Segoe UI|false|false", "segoeui.ttf"   },
                { "Segoe UI|true|false",  "segoeuib.ttf"  },
                { "Segoe UI|false|true",  "segoeuii.ttf"  },
                { "Segoe UI|true|true",   "segoeuiz.ttf"  },
                // Calibri
                { "Calibri|false|false", "calibri.ttf"   },
                { "Calibri|true|false",  "calibrib.ttf"  },
                { "Calibri|false|true",  "calibrii.ttf"  },
                { "Calibri|true|true",   "calibriz.ttf"  },
                // Courier New
                { "Courier New|false|false", "cour.ttf"  },
                { "Courier New|true|false",  "courbd.ttf" },
                { "Courier New|false|true",  "couri.ttf"  },
                { "Courier New|true|true",   "courbi.ttf" },
            };

        private static string BuildKey(string family, bool bold, bool italic)
            => $"{family}|{bold.ToString().ToLower()}|{italic.ToString().ToLower()}";

        private static string FindFontFile(string key)
        {
            if (FaceMap.TryGetValue(key, out string file))
                return Path.Combine(WindowsFontsDir, file);

            // key IS already the filename stem (e.g. "Arial|false|false")
            // — parse it back and try a best-effort match
            var parts = key.Split('|');
            if (parts.Length == 3)
            {
                bool bold   = parts[1].Equals("true",  StringComparison.OrdinalIgnoreCase);
                bool italic = parts[2].Equals("true",  StringComparison.OrdinalIgnoreCase);
                string family = parts[0];

                // Try plain family name as filename (e.g. "times.ttf")
                string guess = Path.Combine(WindowsFontsDir,
                    family.Replace(" ", "").ToLower() +
                    (bold && italic ? "bi" : bold ? "bd" : italic ? "i" : "") + ".ttf");
                if (File.Exists(guess)) return guess;

                // Last resort: any .ttf whose name starts with family stem
                string stem = family.Replace(" ", "").ToLower().Substring(0, Math.Min(5, family.Length));
                foreach (var f in Directory.GetFiles(WindowsFontsDir, stem + "*.ttf"))
                    return f;
            }
            return null;
        }
    }
}
