namespace Application.Common;

public static class DefaultCodeTemplates
{
    public static string Resolve(int languageId, string? template) =>
        string.IsNullOrWhiteSpace(template) ? Get(languageId) : template;

    private static string Get(int languageId) => (languageId switch
    {
        48 or 49 or 50 or 75 or 103 or 104 or 110 => """
            #include <stdio.h>

            int main(void) {
                return 0;
            }
            """,
        52 or 53 or 54 or 76 or 105 => """
            #include <iostream>

            int main() {
                std::ios::sync_with_stdio(false);
                std::cin.tie(nullptr);

                return 0;
            }
            """,
        51 => """
            using System;

            public class Program
            {
                public static void Main(string[] args)
                {
                }
            }
            """,
        62 or 91 => """
            import java.io.*;
            import java.util.*;

            public class Main {
                public static void main(String[] args) throws Exception {
                }
            }
            """,
        70 or 71 or 92 or 100 or 109 or 113 => """
            import sys


            def main():
                pass


            if __name__ == "__main__":
                main()
            """,
        63 or 93 or 97 or 102 => """
            'use strict';

            function main() {
            }

            main();
            """,
        74 or 94 or 101 => """
            function main(): void {
            }

            main();
            """,
        60 or 95 or 106 or 107 => """
            package main

            func main() {
            }
            """,
        78 or 111 => """
            fun main() {
            }
            """,
        73 or 108 => """
            fn main() {
            }
            """,
        90 => """
            import 'dart:io';

            void main() {
            }
            """,
        81 or 112 => """
            object Main {
                def main(args: Array[String]): Unit = {
                }
            }
            """,
        83 => """
            import Foundation

            func main() {
            }

            main()
            """,
        79 => """
            #import <Foundation/Foundation.h>

            int main(int argc, const char *argv[]) {
                @autoreleasepool {
                }
                return 0;
            }
            """,
        84 => """
            Imports System

            Module Program
                Sub Main()
                End Sub
            End Module
            """,
        56 => """
            import std.stdio;

            void main() {
            }
            """,
        68 or 98 => """
            <?php

            function main() {
            }

            main();
            """,
        72 => """
            def main
            end

            main
            """,
        88 => """
            class Main {
                static void main(String[] args) {
                }
            }
            """,
        87 => """
            let main () =
                ()

            main ()
            """,
        61 => """
            main :: IO ()
            main = return ()
            """,
        67 => """
            program Main;
            begin
            end.
            """,
        _ => ""
    }) is { Length: > 0 } source ? source + "\n" : "";
}
