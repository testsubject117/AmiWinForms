Imports System.IO

''' <summary>
''' Command-line tool to compare multiple INVOICE.CHK files
''' Specifically comparing April 2024, October 2025, January 2026, and current production
''' </summary>
Module InvoiceChkComparisonTool

    Sub Main(args As String())
        Console.WriteLine("=" * 80)
        Console.WriteLine("INVOICE.CHK MULTI-FILE COMPARISON TOOL")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        ' Define the files to compare
        Dim files As New Dictionary(Of String, String) From {
            {"April 2024 (Pre-Issue)", "Z:\AM\Active Magnetic Files, Drivers Etc\Backups\Virtualbox VMs\04-08-2024 - Invoice Running\INVXCOPY\INVOICE.CHK"},
            {"October 2025 (First Broken)", "Z:\AM\Active Magnetic Invoice BU Oct. 17, 2025\INVOICE.CHK"},
            {"January 2026 (Merge Day)", "C:\Users\CapnKirk\Downloads\Invoice.chk Files\INVOICE(Jan05-2026).CHK"},
            {"May 2026 (Possible)", "C:\Users\CapnKirk\Downloads\Invoice.chk Files\Possible Invoice.chk\INVOICE.CHK"},
            {"June 2026 (Current Prod)", "C:\Users\CapnKirk\Downloads\Invoice.chk Files\Invoice.chk from Prod Invoice\INVOICE.CHK"}
        }

        ' Verify files exist
        Console.WriteLine("Verifying files...")
        Dim validFiles As New List(Of KeyValuePair(Of String, String))
        For Each kvp In files
            If File.Exists(kvp.Value) Then
                Console.WriteLine($"  ✅ {kvp.Key}")
                validFiles.Add(kvp)
            Else
                Console.WriteLine($"  ❌ {kvp.Key} - NOT FOUND: {kvp.Value}")
            End If
        Next
        Console.WriteLine()

        If validFiles.Count = 0 Then
            Console.WriteLine("ERROR: No valid files found!")
            Console.ReadLine()
            Return
        End If

        ' Scan each file
        Dim scanner As New InvoiceChkScanner()
        Dim results As New Dictionary(Of String, InvoiceChkScanner.ScanResult)

        For Each kvp In validFiles
            Console.WriteLine()
            Console.WriteLine("=" * 80)
            Console.WriteLine($"SCANNING: {kvp.Key}")
            Console.WriteLine("=" * 80)
            Console.WriteLine()

            Dim scanResult As InvoiceChkScanner.ScanResult = scanner.ScanFile(kvp.Value)
            results.Add(kvp.Key, scanResult)

            Console.WriteLine()
            Console.WriteLine("Press Enter to continue to next file...")
            Console.ReadLine()
        Next

        ' Generate comparison summary
        Console.WriteLine()
        Console.WriteLine()
        Console.WriteLine("=" * 80)
        Console.WriteLine("CROSS-FILE COMPARISON SUMMARY")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        Console.WriteLine("File Metadata:")
        Console.WriteLine("=" * 80)
        For Each kvp In results
            Console.WriteLine($"{kvp.Key}:")
            Console.WriteLine($"  Last Modified: {kvp.Value.LastModified}")
            Console.WriteLine($"  File Size: {kvp.Value.FileSize:N0} bytes")
            Console.WriteLine($"  Total Records: {kvp.Value.RecordCount:N0}")
            Console.WriteLine($"  Max Invoice: {kvp.Value.MaxInvoiceNumber}")
            Console.WriteLine($"  Corrupted in Range: {kvp.Value.CorruptionCount}/21")
            Console.WriteLine($"  Unpaid in Range: {kvp.Value.UnpaidInCorruptionRange}")
            Console.WriteLine()
        Next

        ' Compare corruption range across all files
        Console.WriteLine()
        Console.WriteLine("Corruption Range Comparison (391400-391420):")
        Console.WriteLine("=" * 80)

        For invoice As Integer = 391400 To 391420
            Console.WriteLine($"Invoice #{invoice}:")
            For Each kvp In results
                Dim record = kvp.Value.CorruptionRangeRecords.FirstOrDefault(Function(r) r.InvoiceNumber = invoice)
                If record.InvoiceNumber = invoice Then
                    Dim status = If(record.IsValid, "✅", "❌")
                    Dim co = If(record.IsValid, record.CompanyCode.Trim().PadRight(8), "CORRUPT".PadRight(8))
                    Dim amt = If(record.IsValid, $"${record.Amount,10:F2}", "      N/A")
                    Dim flag = If(record.IsValid, record.Flag, "?"c)
                    Console.WriteLine($"  {status} {kvp.Key.PadRight(25)}: [{flag}] {co} {amt}")
                Else
                    Console.WriteLine($"  ❓ {kvp.Key.PadRight(25)}: NOT SCANNED")
                End If
            Next
            Console.WriteLine()
        Next

        ' Analysis
        Console.WriteLine()
        Console.WriteLine("=" * 80)
        Console.WriteLine("KEY FINDINGS")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        ' Check if April 2024 is clean
        Dim april2024 = results.FirstOrDefault(Function(r) r.Key.Contains("April 2024"))
        If april2024.Key IsNot Nothing AndAlso april2024.Value.CorruptionCount = 0 Then
            Console.WriteLine("✅ April 2024 file is CLEAN in corruption range (good merge candidate)")
        ElseIf april2024.Key IsNot Nothing Then
            Console.WriteLine($"❌ April 2024 file has {april2024.Value.CorruptionCount} corrupted records")
        End If

        ' Check if October 2025 is corrupted
        Dim oct2025 = results.FirstOrDefault(Function(r) r.Key.Contains("October 2025"))
        If oct2025.Key IsNot Nothing AndAlso oct2025.Value.CorruptionCount > 0 Then
            Console.WriteLine($"❌ October 2025 file has {oct2025.Value.CorruptionCount} corrupted records (confirms issue at time of problem)")
        ElseIf oct2025.Key IsNot Nothing Then
            Console.WriteLine("✅ October 2025 file is CLEAN in corruption range")
        End If

        ' Check January 2026 (merge day)
        Dim jan2026 = results.FirstOrDefault(Function(r) r.Key.Contains("January 2026"))
        If jan2026.Key IsNot Nothing Then
            Console.WriteLine($"📅 January 2026 (merge day) has {jan2026.Value.CorruptionCount} corrupted records")
        End If

        ' Check current production
        Dim current = results.FirstOrDefault(Function(r) r.Key.Contains("Current Prod"))
        If current.Key IsNot Nothing Then
            Console.WriteLine($"📌 Current Production has {current.Value.CorruptionCount} corrupted records")
        End If

        Console.WriteLine()
        Console.WriteLine("=" * 80)
        Console.WriteLine("Scan complete. Press Enter to exit...")
        Console.ReadLine()
    End Sub

End Module

