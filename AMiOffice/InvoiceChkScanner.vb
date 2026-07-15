Imports System.IO
Imports System.Text

Public Class InvoiceChkScanner
    Private Const RECORD_SIZE As Integer = 26
    Private Const BASE_INVOICE As Integer = 75000

    ' MBF (Microsoft Binary Format) conversion helpers
    Private Function MKS_ToSingle(bytes As Byte()) As Single
        ' Convert 4-byte MKS$ format to Single
        If bytes.Length < 4 Then Return 0.0F

        ' MKS$ format: Sign/Exponent, Mantissa bytes 0-2
        Dim exponent As Integer = bytes(3)
        If exponent = 0 Then Return 0.0F ' Zero value

        ' Reconstruct IEEE 754 single precision
        Dim mantissa As Integer = (bytes(2) << 16) Or (bytes(1) << 8) Or bytes(0)
        mantissa = mantissa Or &H800000 ' Add implicit bit

        Dim ieeeExponent As Integer = exponent - 128 + 127
        Dim ieeeBytes(3) As Byte

        ieeeBytes(3) = CByte((ieeeExponent << 1) Or ((mantissa >> 23) And 1))
        ieeeBytes(2) = CByte((mantissa >> 15) And &HFF)
        ieeeBytes(1) = CByte((mantissa >> 7) And &HFF)
        ieeeBytes(0) = CByte((mantissa << 1) And &HFF)

        Return BitConverter.ToSingle(ieeeBytes, 0)
    End Function

    Private Function MKD_ToDouble(bytes As Byte()) As Double
        ' Convert 8-byte MKD$ format to Double
        If bytes.Length < 8 Then Return 0.0

        ' MKD$ format similar to MKS$ but 8 bytes
        Dim exponent As Integer = bytes(7)
        If exponent = 0 Then Return 0.0 ' Zero value

        ' Reconstruct IEEE 754 double precision
        Dim mantissa As Long = 0
        For i As Integer = 0 To 6
            mantissa = mantissa Or (CLng(bytes(i)) << (i * 8))
        Next
        mantissa = mantissa Or &H10000000000000L ' Add implicit bit

        Dim ieeeExponent As Integer = exponent - 128 + 1023
        Dim ieeeBytes(7) As Byte

        ' Pack into IEEE format
        Dim ieeeLong As Long = (CLng(ieeeExponent) << 52) Or (mantissa And &HFFFFFFFFFFFFFL)
        ieeeBytes = BitConverter.GetBytes(ieeeLong)

        Return BitConverter.ToDouble(ieeeBytes, 0)
    End Function

    Public Structure InvoiceRecord
        Public RecordNumber As Integer
        Public InvoiceNumber As Integer
        Public Amount As Double
        Public CompanyCode As String
        Public Flag As Char
        Public IsValid As Boolean
        Public ValidationIssues As String
        Public RawBytes As Byte()
    End Structure

    Public Function ScanFile(filePath As String) As ScanResult
        Dim result As New ScanResult With {
            .FilePath = filePath,
            .ScanDate = DateTime.Now
        }

        Try
            Dim fileInfo As New FileInfo(filePath)
            result.FileSize = fileInfo.Length
            result.LastModified = fileInfo.LastWriteTime
            result.RecordCount = fileInfo.Length \ RECORD_SIZE
            result.MaxInvoiceNumber = result.RecordCount + BASE_INVOICE

            Console.WriteLine($"Scanning: {filePath}")
            Console.WriteLine($"  File size: {result.FileSize:N0} bytes")
            Console.WriteLine($"  Modified: {result.LastModified}")
            Console.WriteLine($"  Records: {result.RecordCount:N0}")
            Console.WriteLine($"  Max invoice: {result.MaxInvoiceNumber}")
            Console.WriteLine()

            ' Scan corruption range 391400-391420
            Console.WriteLine("Scanning corruption range (391400-391420)...")
            For invoice As Integer = 391400 To 391420
                Dim record As InvoiceRecord = ReadRecord(filePath, invoice)
                result.CorruptionRangeRecords.Add(record)

                If Not record.IsValid Then
                    result.CorruptionCount += 1
                    Console.WriteLine($"  ❌ {invoice}: {record.ValidationIssues}")
                Else
                    Console.WriteLine($"  ✅ {invoice}: {record.CompanyCode.Trim()} ${record.Amount:F2} [{record.Flag}]")
                End If
            Next
            Console.WriteLine()

            ' Sample records before and after
            Console.WriteLine("Sampling records before corruption (391390-391399)...")
            For invoice As Integer = 391390 To 391399
                Dim record As InvoiceRecord = ReadRecord(filePath, invoice)
                result.PreCorruptionSample.Add(record)

                Dim status As String = If(record.IsValid, "✅", "❌")
                Console.WriteLine($"  {status} {invoice}: {record.CompanyCode.Trim()} ${record.Amount:F2} [{record.Flag}]")
            Next
            Console.WriteLine()

            Console.WriteLine("Sampling records after corruption (391421-391430)...")
            For invoice As Integer = 391421 To 391430
                Dim record As InvoiceRecord = ReadRecord(filePath, invoice)
                result.PostCorruptionSample.Add(record)

                Dim status As String = If(record.IsValid, "✅", "❌")
                Console.WriteLine($"  {status} {invoice}: {record.CompanyCode.Trim()} ${record.Amount:F2} [{record.Flag}]")
            Next
            Console.WriteLine()

            ' Count unpaid invoices in corruption range
            result.UnpaidInCorruptionRange = result.CorruptionRangeRecords.Count(Function(r) r.Flag = "J"c)

            ' Statistics
            Console.WriteLine("Summary:")
            Console.WriteLine($"  Corrupted records in range: {result.CorruptionCount}/21")
            Console.WriteLine($"  Unpaid (FLAG=J) in corruption range: {result.UnpaidInCorruptionRange}")
            Console.WriteLine()

            result.Success = True

        Catch ex As Exception
            result.Success = False
            result.ErrorMessage = ex.Message
            Console.WriteLine($"❌ Error scanning file: {ex.Message}")
        End Try

        Return result
    End Function

    Private Function ReadRecord(filePath As String, invoiceNumber As Integer) As InvoiceRecord
        Dim record As New InvoiceRecord With {
            .InvoiceNumber = invoiceNumber,
            .RecordNumber = invoiceNumber - BASE_INVOICE,
            .IsValid = True,
            .ValidationIssues = ""
        }

        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using reader As New BinaryReader(fs)
                    Dim offset As Long = CLng(record.RecordNumber) * RECORD_SIZE

                    If offset >= fs.Length Then
                        record.IsValid = False
                        record.ValidationIssues = "Record beyond EOF"
                        Return record
                    End If

                    fs.Seek(offset, SeekOrigin.Begin)
                    record.RawBytes = reader.ReadBytes(RECORD_SIZE)

                    If record.RawBytes.Length < RECORD_SIZE Then
                        record.IsValid = False
                        record.ValidationIssues = "Incomplete record"
                        Return record
                    End If

                    ' Parse INUM$ (bytes 0-8) - should match record number
                    Dim inumBytes(3) As Byte
                    Array.Copy(record.RawBytes, 0, inumBytes, 0, 4)
                    Try
                        Dim storedRecordNum As Single = MKS_ToSingle(inumBytes)
                        If Math.Abs(storedRecordNum - record.RecordNumber) > 0.5 Then
                            record.IsValid = False
                            record.ValidationIssues &= $"INUM mismatch (expected {record.RecordNumber}, got {storedRecordNum}); "
                        End If
                    Catch
                        record.IsValid = False
                        record.ValidationIssues &= "INUM$ decode failed; "
                    End Try

                    ' Parse AMT$ (bytes 9-16)
                    Dim amtBytes(7) As Byte
                    Array.Copy(record.RawBytes, 9, amtBytes, 0, 8)
                    Try
                        record.Amount = MKD_ToDouble(amtBytes)
                        If record.Amount < 0 OrElse record.Amount > 999999 Then
                            record.IsValid = False
                            record.ValidationIssues &= $"Amount suspicious ({record.Amount:F2}); "
                        End If
                    Catch
                        record.IsValid = False
                        record.ValidationIssues &= "AMT$ decode failed; "
                    End Try

                    ' Parse CO$ (bytes 17-24)
                    Dim coBytes(7) As Byte
                    Array.Copy(record.RawBytes, 17, coBytes, 0, 8)
                    record.CompanyCode = Encoding.ASCII.GetString(coBytes)

                    ' Validate company code (should be alphanumeric + space)
                    If Not System.Text.RegularExpressions.Regex.IsMatch(record.CompanyCode, "^[A-Z0-9 ]{8}$") Then
                        record.IsValid = False
                        record.ValidationIssues &= $"Invalid CO$ ('{record.CompanyCode.Replace(" "c, "·"c)}'); "
                    End If

                    ' Parse FLAG$ (byte 25)
                    record.Flag = Chr(record.RawBytes(25))
                    If Not "PJCVE".Contains(record.Flag) Then
                        record.IsValid = False
                        record.ValidationIssues &= $"Invalid FLAG ('{record.Flag}' = {record.RawBytes(25)}); "
                    End If

                End Using
            End Using

        Catch ex As Exception
            record.IsValid = False
            record.ValidationIssues = $"Read error: {ex.Message}"
        End Try

        Return record
    End Function

    Public Function CompareFiles(file1Path As String, file2Path As String) As ComparisonResult
        Console.WriteLine("=" * 80)
        Console.WriteLine("COMPARING TWO INVOICE.CHK FILES")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        Console.WriteLine("FILE 1 (Earlier):")
        Dim scan1 As ScanResult = ScanFile(file1Path)

        Console.WriteLine()
        Console.WriteLine("FILE 2 (Later):")
        Dim scan2 As ScanResult = ScanFile(file2Path)

        Console.WriteLine()
        Console.WriteLine("=" * 80)
        Console.WriteLine("COMPARISON ANALYSIS")
        Console.WriteLine("=" * 80)
        Console.WriteLine()

        Dim comparison As New ComparisonResult With {
            .File1 = scan1,
            .File2 = scan2
        }

        ' Compare corruption range
        Console.WriteLine("Corruption Range Comparison (391400-391420):")
        Console.WriteLine()
        Console.WriteLine("Invoice# | File1 Status | File2 Status | File1 Company | File2 Company | Match?")
        Console.WriteLine("-" * 90)

        For i As Integer = 0 To 20
            Dim rec1 = scan1.CorruptionRangeRecords(i)
            Dim rec2 = scan2.CorruptionRangeRecords(i)

            Dim status1 = If(rec1.IsValid, "✅ Valid", "❌ Corrupt")
            Dim status2 = If(rec2.IsValid, "✅ Valid", "❌ Corrupt")
            Dim match = If(rec1.IsValid = rec2.IsValid AndAlso rec1.CompanyCode = rec2.CompanyCode, "✓", "✗")

            Console.WriteLine($"{rec1.InvoiceNumber,8} | {status1,12} | {status2,12} | {rec1.CompanyCode.Trim(),13} | {rec2.CompanyCode.Trim(),13} | {match}")

            If rec1.IsValid <> rec2.IsValid Then
                comparison.DifferenceCount += 1
            End If
        Next

        Console.WriteLine()
        Console.WriteLine($"Records different between files: {comparison.DifferenceCount}/21")
        Console.WriteLine()

        ' Determine best file
        If scan1.CorruptionCount = 0 AndAlso scan2.CorruptionCount > 0 Then
            comparison.Recommendation = "Use FILE 1 (earlier) - has valid data in corruption range"
            Console.WriteLine("✅ RECOMMENDATION: Use FILE 1 for merge (corruption range is clean)")
        ElseIf scan2.CorruptionCount = 0 AndAlso scan1.CorruptionCount > 0 Then
            comparison.Recommendation = "Use FILE 2 (later) - has valid data in corruption range"
            Console.WriteLine("✅ RECOMMENDATION: Use FILE 2 for merge (corruption range is clean)")
        ElseIf scan1.CorruptionCount = 0 AndAlso scan2.CorruptionCount = 0 Then
            comparison.Recommendation = "Both files clean - use either for corruption range"
            Console.WriteLine("✅ RECOMMENDATION: Both files have valid corruption range - use either")
        Else
            comparison.Recommendation = "Both files corrupted - use erase strategy"
            Console.WriteLine("⚠️ RECOMMENDATION: Both files corrupted in range - mark as erased")
        End If

        Console.WriteLine()

        ' Check if either has unpaid in corruption range
        If scan1.UnpaidInCorruptionRange > 0 OrElse scan2.UnpaidInCorruptionRange > 0 Then
            Console.WriteLine("⚠️ WARNING: Unpaid invoices (FLAG=J) found in corruption range!")
            Console.WriteLine($"   File 1: {scan1.UnpaidInCorruptionRange} unpaid")
            Console.WriteLine($"   File 2: {scan2.UnpaidInCorruptionRange} unpaid")
            Console.WriteLine("   These would appear in monthly report if readable")
            Console.WriteLine()
        End If

        Return comparison
    End Function

    Public Sub ExportDetailedReport(scan As ScanResult, outputPath As String)
        Using writer As New StreamWriter(outputPath)
            writer.WriteLine("INVOICE.CHK DETAILED SCAN REPORT")
            writer.WriteLine("=" * 80)
            writer.WriteLine($"Scan Date: {scan.ScanDate}")
            writer.WriteLine($"File: {scan.FilePath}")
            writer.WriteLine($"File Size: {scan.FileSize:N0} bytes")
            writer.WriteLine($"Last Modified: {scan.LastModified}")
            writer.WriteLine($"Record Count: {scan.RecordCount:N0}")
            writer.WriteLine($"Max Invoice: {scan.MaxInvoiceNumber}")
            writer.WriteLine()

            writer.WriteLine("CORRUPTION RANGE (391400-391420)")
            writer.WriteLine("-" * 80)
            writer.WriteLine("Invoice# | Valid? | Company  | Amount      | Flag | Issues")
            writer.WriteLine("-" * 80)

            For Each rec In scan.CorruptionRangeRecords
                Dim valid = If(rec.IsValid, "✅", "❌")
                writer.WriteLine($"{rec.InvoiceNumber,8} | {valid,6} | {rec.CompanyCode,8} | {rec.Amount,11:F2} | {rec.Flag,4} | {rec.ValidationIssues}")
            Next

            writer.WriteLine()
            writer.WriteLine("RAW HEX DUMP OF CORRUPTED RECORDS")
            writer.WriteLine("-" * 80)

            For Each rec In scan.CorruptionRangeRecords
                If Not rec.IsValid Then
                    writer.WriteLine($"Invoice {rec.InvoiceNumber} (Record {rec.RecordNumber}):")
                    writer.WriteLine($"  Hex: {BitConverter.ToString(rec.RawBytes).Replace("-", " ")}")
                    writer.WriteLine($"  ASCII: {Encoding.ASCII.GetString(rec.RawBytes).Replace(ControlChars.NullChar, "·"c)}")
                    writer.WriteLine()
                End If
            Next

        End Using
        Console.WriteLine($"Detailed report exported to: {outputPath}")
    End Sub

    Public Class ScanResult
        Public Property FilePath As String
        Public Property ScanDate As DateTime
        Public Property FileSize As Long
        Public Property LastModified As DateTime
        Public Property RecordCount As Long
        Public Property MaxInvoiceNumber As Integer
        Public Property CorruptionRangeRecords As New List(Of InvoiceRecord)
        Public Property PreCorruptionSample As New List(Of InvoiceRecord)
        Public Property PostCorruptionSample As New List(Of InvoiceRecord)
        Public Property CorruptionCount As Integer
        Public Property UnpaidInCorruptionRange As Integer
        Public Property Success As Boolean
        Public Property ErrorMessage As String
    End Class

    Public Class ComparisonResult
        Public Property File1 As ScanResult
        Public Property File2 As ScanResult
        Public Property DifferenceCount As Integer
        Public Property Recommendation As String
    End Class
End Class

