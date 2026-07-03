using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;

namespace PremiumLivingOPS.Services
{
    /// <summary>
    /// PdfSharp 6.x IFontResolver that reads TTF files directly from
    /// %WINDIR%\Fonts. Supports Arial, Segoe UI, Calibri, Courier New
    /// in all four faces (Regular / Bold / Italic / BoldItalic).
    ///
    /// Usage: call EnsureRegistered() once before creating any XFont.
    /// </summary>
    public sealed class WindowsFontResolver : IFontResolver
    {
        private static bool _registered = false;
        private static readonly object _lock = new object();

        /// <summary>Thread-safe, idempotent registration.</summary>
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

        private static readonly string FontsDir =
            Path.Combine(
                Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows",
                "Fonts");

        // key = "FamilyName|bold|italic"  (lower-case bool strings)
        private static readonly Dictionary<string, string> FaceMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Arial|false|false",       "arial.ttf"    },
                { "Arial|true|false",        "arialbd.ttf"  },
                { "Arial|false|true",        "ariali.ttf"   },
                { "Arial|true|true",         "arialbi.ttf"  },
                { "Segoe UI|false|false",    "segoeui.ttf"  },
                { "Segoe UI|true|false",     "segoeuib.ttf" },
                { "Segoe UI|false|true",     "segoeuii.ttf" },
                { "Segoe UI|true|true",      "segoeuiz.ttf" },
                { "Calibri|false|false",     "calibri.ttf"  },
                { "Calibri|true|false",      "calibrib.ttf" },
                { "Calibri|false|true",      "calibrii.ttf" },
                { "Calibri|true|true",       "calibriz.ttf" },
                { "Courier New|false|false", "cour.ttf"     },
                { "Courier New|true|false",  "courbd.ttf"   },
                { "Courier New|false|true",  "couri.ttf"    },
                { "Courier New|true|true",   "courbi.ttf"   },
            };

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // The key returned here becomes the faceName passed to GetFont.
            string key = $"{familyName}|{isBold.ToString().ToLower()}|{isItalic.ToString().ToLower()}";
            return new FontResolverInfo(key);
        }

        public byte[] GetFont(string faceName)
        {
            if (FaceMap.TryGetValue(faceName, out string fileName))
            {
                string path = Path.Combine(FontsDir, fileName);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }

            // Absolute fallback: arial.ttf
            string fallback = Path.Combine(FontsDir, "arial.ttf");
            if (File.Exists(fallback))
                return File.ReadAllBytes(fallback);

            throw new FileNotFoundException(
                $"Font '{faceName}' not found in {FontsDir}. " +
                "Ensure Arial is installed on this machine.");
        }
    }
}
