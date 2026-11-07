export const languages = [
  { id: 71, name: 'Python (3.8.1)', extension: '.py' },
  { id: 92, name: 'Python (3.11.2)', extension: '.py' },
  { id: 100, name: 'Python (3.12.5)', extension: '.py' },
  { id: 109, name: 'Python (3.13.2)', extension: '.py' },
  { id: 113, name: 'Python (3.14.0)', extension: '.py' },
  { id: 70, name: 'Python (2.7.17)', extension: '.py' },
  { id: 54, name: 'C++ (GCC 9.2.0)', extension: '.cpp' },
  { id: 52, name: 'C++ (GCC 7.4.0)', extension: '.cpp' },
  { id: 53, name: 'C++ (GCC 8.3.0)', extension: '.cpp' },
  { id: 76, name: 'C++ (Clang 7.0.1)', extension: '.cpp' },
  { id: 105, name: 'C++ (GCC 14.1.0)', extension: '.cpp' },
  { id: 51, name: 'C# (Mono 6.6.0.161)', extension: '.cs' },
  { id: 63, name: 'JavaScript (Node.js 12.14.0)', extension: '.js' },
  { id: 93, name: 'JavaScript (Node.js 18.15.0)', extension: '.js' },
  { id: 97, name: 'JavaScript (Node.js 20.17.0)', extension: '.js' },
  { id: 102, name: 'JavaScript (Node.js 22.08.0)', extension: '.js' },
  { id: 60, name: 'Go (1.13.5)', extension: '.go' },
  { id: 95, name: 'Go (1.18.5)', extension: '.go' },
  { id: 106, name: 'Go (1.22.0)', extension: '.go' },
  { id: 107, name: 'Go (1.23.5)', extension: '.go' },
  { id: 48, name: 'C (GCC 7.4.0)', extension: '.c' },
  { id: 49, name: 'C (GCC 8.3.0)', extension: '.c' },
  { id: 50, name: 'C (GCC 9.2.0)', extension: '.c' },
  { id: 75, name: 'C (Clang 7.0.1)', extension: '.c' },
  { id: 103, name: 'C (GCC 14.1.0)', extension: '.c' },
  { id: 104, name: 'C (Clang 18.1.8)', extension: '.c' },
  { id: 110, name: 'C (Clang 19.1.7)', extension: '.c' },
];
export const difficultyNames = { Easy: 'Лёгкая', Medium: 'Средняя', Hard: 'Сложная' };
export function starter(id: number): string {
  const extension = languages.find(x => x.id === id)?.extension;
  switch (extension) {
    case '.py': return '# Всё начинается с первой строки\na, b = map(int, input().split())\nprint(a + b)\n';
    case '.js': return "const fs = require('fs');\nconst [a, b] = fs.readFileSync(0, 'utf8').trim().split(/\\s+/).map(Number);\nconsole.log(a + b);\n";
    case '.cpp': return '#include <iostream>\n\nint main() {\n    long long a, b;\n    std::cin >> a >> b;\n    std::cout << a + b << "\\n";\n}\n';
    case '.c': return '#include <stdio.h>\n\nint main(void) {\n    long long a, b;\n    scanf("%lld %lld", &a, &b);\n    printf("%lld\\n", a + b);\n    return 0;\n}\n';
    case '.cs': return 'using System;\n\nclass Program {\n    static void Main() {\n        var values = Console.ReadLine().Split();\n        Console.WriteLine(long.Parse(values[0]) + long.Parse(values[1]));\n    }\n}\n';
    default: return 'package main\n\nimport "fmt"\n\nfunc main() {\n    var a, b int64\n    fmt.Scan(&a, &b)\n    fmt.Println(a + b)\n}\n';
  }
}
