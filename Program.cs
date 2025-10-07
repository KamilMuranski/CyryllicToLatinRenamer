using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CyryllicToLatinRenamer
{
    class Program
    {
        // Wzorce nazw
        static readonly Regex AlbumPattern = new Regex(@"^(\d{4})\s-\s(.+)$", RegexOptions.Compiled);
        static readonly Regex TrackPattern = new Regex(@"^(\d{2})\s-\s(.+)$", RegexOptions.Compiled);

        static void Main()
        {
            // Wymuś UTF-8 w konsoli
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            Console.InputEncoding = Encoding.UTF8;

            // EXE stoi na poziomie folderów gatunków
            string root = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                foreach (var genreDir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                    foreach (var bandDir in Directory.GetDirectories(genreDir, "*", SearchOption.TopDirectoryOnly))
                        foreach (var albumDir in Directory.GetDirectories(bandDir, "*", SearchOption.TopDirectoryOnly))
                        {
                            // 1) Zmień nazwę folderu albumu
                            string currentAlbumPath = RenameAlbumFolder(albumDir);

                            // 2) Zmień nazwy wszystkich plików wewnątrz albumu (rekurencyjnie), ale nie folderów
                            RenameFiles(currentAlbumPath);
                        }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Błąd ogólny: {ex.Message}");
            }
        }

        static string RenameAlbumFolder(string albumDir)
        {
            string parent = Path.GetDirectoryName(albumDir) ?? "";
            string folderName = Path.GetFileName(albumDir);

            var m = AlbumPattern.Match(folderName);
            if (!m.Success) return albumDir; // nie wygląda na "YYYY - Tytuł"

            string year = m.Groups[1].Value;
            string titleCyr = m.Groups[2].Value;

            string titleLat = TransliterateCyrillic(titleCyr);

            // jeśli transliteracja nic nie zmienia – nie ruszaj folderu
            if (string.Equals(titleLat, titleCyr, StringComparison.Ordinal))
                return albumDir;

            string newName = $"{year} - {titleLat} ({titleCyr})";
            string newPath = Path.Combine(parent, newName);

            if (!string.Equals(albumDir, newPath, StringComparison.Ordinal))
            {
                try
                {
                    Directory.Move(albumDir, newPath);
                    return newPath;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Błąd zmiany nazwy folderu '{albumDir}' -> '{newPath}': {ex.Message}");
                    return albumDir; // kontynuuj z oryginalną ścieżką
                }
            }

            return albumDir;
        }

        static void RenameFiles(string albumDir)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.GetFiles(albumDir, "*.*", SearchOption.AllDirectories)
                                 .Where(IsSupportedFile)
                                 .ToList();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Błąd listowania plików w '{albumDir}': {ex.Message}");
                return;
            }

            foreach (var path in files)
            {
                string dir = Path.GetDirectoryName(path) ?? "";
                string fileName = Path.GetFileName(path);
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);

                string newFileName = fileName; // domyślnie bez zmian

                var t = TrackPattern.Match(nameNoExt);
                if (t.Success)        // 03 - Tytuł
                {
                    string nn = t.Groups[1].Value;
                    string titleCyr = t.Groups[2].Value;
                    string titleLat = TransliterateCyrillic(titleCyr);

                    if (!string.Equals(titleLat, titleCyr, StringComparison.Ordinal))
                        newFileName = $"{nn} - {titleLat} ({titleCyr}){ext}";
                }
                else                   // okładki/obrazy i inne pliki
                {
                    string titleCyr = nameNoExt;
                    string titleLat = TransliterateCyrillic(titleCyr);

                    if (!string.Equals(titleLat, titleCyr, StringComparison.Ordinal))
                        newFileName = $"{titleLat} ({titleCyr}){ext}";
                }

                string newPath = Path.Combine(dir, newFileName);
                if (!string.Equals(path, newPath, StringComparison.Ordinal))
                {
                    try
                    {
                        File.Move(path, newPath);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Błąd zmiany nazwy pliku '{path}' -> '{newPath}': {ex.Message}");
                    }
                }
            }
        }

        static bool IsSupportedFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mp3" || ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }

        /// <summary>
        /// Prosta transliteracja cyrylica → łacinka.
        /// </summary>
        static string TransliterateCyrillic(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var map = new Dictionary<char, string>
            {
                // wielkie
                ['А'] = "A",
                ['Б'] = "B",
                ['В'] = "V",
                ['Г'] = "G",
                ['Д'] = "D",
                ['Е'] = "E",
                ['Ё'] = "Yo",
                ['Ж'] = "Zh",
                ['З'] = "Z",
                ['И'] = "I",
                ['Й'] = "I",
                ['К'] = "K",
                ['Л'] = "L",
                ['М'] = "M",
                ['Н'] = "N",
                ['О'] = "O",
                ['П'] = "P",
                ['Р'] = "R",
                ['С'] = "S",
                ['Т'] = "T",
                ['У'] = "U",
                ['Ф'] = "F",
                ['Х'] = "Kh",
                ['Ц'] = "Ts",
                ['Ч'] = "Ch",
                ['Ш'] = "Sh",
                ['Щ'] = "Shch",
                ['Ъ'] = "",
                ['Ы'] = "Y",
                ['Ь'] = "’",
                ['Э'] = "E",
                ['Ю'] = "Yu",
                ['Я'] = "Ya",
                // małe
                ['а'] = "a",
                ['б'] = "b",
                ['в'] = "v",
                ['г'] = "g",
                ['д'] = "d",
                ['е'] = "e",
                ['ё'] = "yo",
                ['ж'] = "zh",
                ['з'] = "z",
                ['и'] = "i",
                ['й'] = "i",
                ['к'] = "k",
                ['л'] = "l",
                ['м'] = "m",
                ['н'] = "n",
                ['о'] = "o",
                ['п'] = "p",
                ['р'] = "r",
                ['с'] = "s",
                ['т'] = "t",
                ['у'] = "u",
                ['ф'] = "f",
                ['х'] = "kh",
                ['ц'] = "ts",
                ['ч'] = "ch",
                ['ш'] = "sh",
                ['щ'] = "shch",
                ['ъ'] = "",
                ['ы'] = "y",
                ['ь'] = "’",
                ['э'] = "e",
                ['ю'] = "yu",
                ['я'] = "ya",
                // ukraińskie
                ['І'] = "I",
                ['Ї'] = "Yi",
                ['Є'] = "Ye",
                ['Ґ'] = "G",
                ['і'] = "i",
                ['ї'] = "yi",
                ['є'] = "ye",
                ['ґ'] = "g",
                // białoruskie
                ['Ў'] = "U",
                ['ў'] = "u"
            };

            var sb = new StringBuilder(input.Length * 2);
            foreach (var ch in input)
                sb.Append(map.TryGetValue(ch, out var latin) ? latin : ch);
            return sb.ToString();
        }
    }
}
