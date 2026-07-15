Imports System.IO

Module InvoiceChkScannerTest
    Sub Main()
        Console.WriteLine("INVOICE.CHK FILE SCANNER")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        ' === SINGLE FILE SCAN ===
        Console.WriteLine("Usage: InvoiceChkScanner <command> <file1> [file2]")
        Console.WriteLine()
        Console.WriteLine("Commands:")
        Console.WriteLine("  scan <file>           - Scan single INVOICE.CHK file")
        Console.WriteLine("  compare <file1> <file2> - Compare two INVOICE.CHK files")
        Console.WriteLine()

        Dim args = Environment.GetCommandLineArgs()

        If args.Length < 3 Then
            Console.WriteLine("Please provide command and file path(s)")
            Console.WriteLine()
            Console.WriteLine("Example single scan:")
            Console.WriteLine("  InvoiceChkScanner scan ""S:\INVOICE.CHK""")
            Console.WriteLine()
            Console.WriteLine("Example comparison:")
            Console.WriteLine("  InvoiceChkScanner compare ""path\to\sept2025\INVOICE.CHK"" ""path\to\oct2025\INVOICE.CHK""")
            Console.WriteLine()
            Console.ReadLine()
            Return
        End If

        Dim command = args(1).ToLower()
        Dim scanner As New InvoiceChkScanner()

        Select Case command
            Case "scan"
                If args.Length < 3 Then
                    Console.WriteLine("Error: Please provide file path for scan")
                    Return
                End If

                Dim filePath = args(2)
                If Not File.Exists(filePath) Then
                    Console.WriteLine($"Error: File not found: {filePath}")
                    Return
                End If

                Console.WriteLine("SINGLE FILE SCAN")
                Console.WriteLine("=" * 80)
                Console.WriteLine()

                Dim result = scanner.ScanFile(filePath)

                If result.Success Then
                    Console.WriteLine()
                    Console.WriteLine("Scan completed successfully!")
                    Console.WriteLine()

                    ' Offer to export detailed report
                    Console.Write("Export detailed report? (y/n): ")
                    Dim response = Console.ReadLine()
                    If response?.ToLower() = "y" Then
                        Dim reportPath = Path.ChangeExtension(filePath, ".scan-report.txt")
                        scanner.ExportDetailedReport(result, reportPath)
                    End If
                Else
                    Console.WriteLine($"Scan failed: {result.ErrorMessage}")
                End If

            Case "compare"
                If args.Length < 4 Then
                    Console.WriteLine("Error: Please provide two file paths for comparison")
                    Return
                End If

                Dim file1 = args(2)
                Dim file2 = args(3)

                If Not File.Exists(file1) Then
                    Console.WriteLine($"Error: File 1 not found: {file1}")
                    Return
                End If

                If Not File.Exists(file2) Then
                    Console.WriteLine($"Error: File 2 not found: {file2}")
                    Return
                End If

                Dim comparison = scanner.CompareFiles(file1, file2)

                Console.WriteLine()
                Console.WriteLine("Comparison completed!")
                Console.WriteLine()

                ' Offer to export reports
                Console.Write("Export detailed reports for both files? (y/n): ")
                Dim response = Console.ReadLine()
                If response?.ToLower() = "y" Then
                    Dim report1 = Path.ChangeExtension(file1, ".scan-report.txt")
                    Dim report2 = Path.ChangeExtension(file2, ".scan-report.txt")
                    scanner.ExportDetailedReport(comparison.File1, report1)
                    scanner.ExportDetailedReport(comparison.File2, report2)
                End If

            Case Else
                Console.WriteLine($"Unknown command: {command}")
                Console.WriteLine("Valid commands: scan, compare")
        End Select

        Console.WriteLine()
        Console.WriteLine("Press Enter to exit...")
        Console.ReadLine()
    End Sub
End Module

