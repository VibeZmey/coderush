using Domain.Models.Problems;

namespace Infrastructure.Persistence.Seeding;

/// <summary>Snapshot of https://ce.judge0.com/languages/ retrieved on 2026-11-03.</summary>
internal static class Judge0LanguageCatalog
{
    public static IEnumerable<ProgrammingLanguage> Languages => Entries.Select(entry => new ProgrammingLanguage
    {
        Id = entry.Id,
        Key = $"judge0-{entry.Id}",
        DisplayName = entry.Name,
        RuntimeVersion = entry.Name.Contains('(') ? entry.Name[(entry.Name.IndexOf('(') + 1)..^1] : "",
        FileExtension = entry.Extension,
        IsEnabled = entry.Name.StartsWith("C (") || entry.Name.StartsWith("C++ (") ||
            entry.Name.StartsWith("C# (") || entry.Name.StartsWith("Python (") ||
            entry.Name.StartsWith("JavaScript (") || entry.Name.StartsWith("Go ("),
        CreatedAt = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero)
    });

    private static readonly (int Id, string Name, string Extension)[] Entries =
    [
        (43, "Plain Text", ".txt"),
        (44, "Executable", ".bin"),
        (45, "Assembly (NASM 2.14.02)", ".asm"),
        (46, "Bash (5.0.0)", ".sh"),
        (47, "Basic (FBC 1.07.1)", ".bas"),
        (48, "C (GCC 7.4.0)", ".c"),
        (49, "C (GCC 8.3.0)", ".c"),
        (50, "C (GCC 9.2.0)", ".c"),
        (51, "C# (Mono 6.6.0.161)", ".cs"),
        (52, "C++ (GCC 7.4.0)", ".cpp"),
        (53, "C++ (GCC 8.3.0)", ".cpp"),
        (54, "C++ (GCC 9.2.0)", ".cpp"),
        (55, "Common Lisp (SBCL 2.0.0)", ".lisp"),
        (56, "D (DMD 2.089.1)", ".d"),
        (57, "Elixir (1.9.4)", ".exs"),
        (58, "Erlang (OTP 22.2)", ".erl"),
        (59, "Fortran (GFortran 9.2.0)", ".f90"),
        (60, "Go (1.13.5)", ".go"),
        (61, "Haskell (GHC 8.8.1)", ".hs"),
        (62, "Java (OpenJDK 13.0.1)", ".java"),
        (63, "JavaScript (Node.js 12.14.0)", ".js"),
        (64, "Lua (5.3.5)", ".lua"),
        (65, "OCaml (4.09.0)", ".ml"),
        (66, "Octave (5.1.0)", ".m"),
        (67, "Pascal (FPC 3.0.4)", ".pas"),
        (68, "PHP (7.4.1)", ".php"),
        (69, "Prolog (GNU Prolog 1.4.5)", ".pro"),
        (70, "Python (2.7.17)", ".py"),
        (71, "Python (3.8.1)", ".py"),
        (72, "Ruby (2.7.0)", ".rb"),
        (73, "Rust (1.40.0)", ".rs"),
        (74, "TypeScript (3.7.4)", ".ts"),
        (75, "C (Clang 7.0.1)", ".c"),
        (76, "C++ (Clang 7.0.1)", ".cpp"),
        (77, "COBOL (GnuCOBOL 2.2)", ".cob"),
        (78, "Kotlin (1.3.70)", ".kt"),
        (79, "Objective-C (Clang 7.0.1)", ".m"),
        (80, "R (4.0.0)", ".r"),
        (81, "Scala (2.13.2)", ".scala"),
        (82, "SQL (SQLite 3.27.2)", ".sql"),
        (83, "Swift (5.2.3)", ".swift"),
        (84, "Visual Basic.Net (vbnc 0.0.0.5943)", ".vb"),
        (85, "Perl (5.28.1)", ".pl"),
        (86, "Clojure (1.10.1)", ".clj"),
        (87, "F# (.NET Core SDK 3.1.202)", ".fs"),
        (88, "Groovy (3.0.3)", ".groovy"),
        (89, "Multi-file program", ".zip"),
        (90, "Dart (2.19.2)", ".dart"),
        (91, "Java (JDK 17.0.6)", ".java"),
        (92, "Python (3.11.2)", ".py"),
        (93, "JavaScript (Node.js 18.15.0)", ".js"),
        (94, "TypeScript (5.0.3)", ".ts"),
        (95, "Go (1.18.5)", ".go"),
        (96, "JavaFX (JDK 17.0.6, OpenJFX 22.0.2)", ".java"),
        (97, "JavaScript (Node.js 20.17.0)", ".js"),
        (98, "PHP (8.3.11)", ".php"),
        (99, "R (4.4.1)", ".r"),
        (100, "Python (3.12.5)", ".py"),
        (101, "TypeScript (5.6.2)", ".ts"),
        (102, "JavaScript (Node.js 22.08.0)", ".js"),
        (103, "C (GCC 14.1.0)", ".c"),
        (104, "C (Clang 18.1.8)", ".c"),
        (105, "C++ (GCC 14.1.0)", ".cpp"),
        (106, "Go (1.22.0)", ".go"),
        (107, "Go (1.23.5)", ".go"),
        (108, "Rust (1.85.0)", ".rs"),
        (109, "Python (3.13.2)", ".py"),
        (110, "C (Clang 19.1.7)", ".c"),
        (111, "Kotlin (2.1.10)", ".kt"),
        (112, "Scala (3.4.2)", ".scala"),
        (113, "Python (3.14.0)", ".py")
    ];
}
