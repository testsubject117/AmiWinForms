Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Microsoft.VisualBasic.FileIO

Public Class RolodexRepository

    Private Shared ReadOnly BaseFolder As String = "\\invoice\mainmenu\data\phone"

    Public Function FindFirstByNamePrefix(search As String) As RolodexPersonRecord
        Dim matches = FindAllByNamePrefix(search)
        If matches.Count = 0 Then Return Nothing
        Return CloneRecord(matches(0))
    End Function

    Public Function FindAllByNamePrefix(search As String) As List(Of RolodexPersonRecord)
        Dim results As New List(Of RolodexPersonRecord)

        If String.IsNullOrWhiteSpace(search) Then Return results

        Dim normalized = search.Trim().ToUpperInvariant()
        Dim filePath = GetFilePathForKey(normalized(0))

        If Not File.Exists(filePath) Then Return results

        For Each record In ReadAllRecordsFromFile(filePath)
            If record Is Nothing Then Continue For
            If String.IsNullOrWhiteSpace(record.PersonName) Then Continue For

            If record.PersonName.Trim().ToUpperInvariant().StartsWith(normalized, StringComparison.Ordinal) Then
                results.Add(CloneRecord(record))
            End If
        Next

        Return results
    End Function

    Public Function GlobalSearch(term As String) As List(Of RolodexPersonRecord)
        Dim results As New List(Of RolodexPersonRecord)

        If String.IsNullOrWhiteSpace(term) Then
            Return results
        End If

        Dim normalized = term.Trim().ToUpperInvariant()

        For Each key In GetAllShardKeys()
            Dim filePath = GetFilePathForKey(key)

            If Not File.Exists(filePath) Then Continue For

            For Each record In ReadAllRecordsFromFile(filePath)
                If record Is Nothing Then Continue For

                If RecordContains(record, normalized) Then
                    results.Add(CloneRecord(record))
                End If
            Next
        Next

        Return results
    End Function

    Public Function AddRecord(record As RolodexPersonRecord) As Boolean
        If record Is Nothing OrElse String.IsNullOrWhiteSpace(record.PersonName) Then Return False

        EnsureBaseFolder()

        Dim filePath = GetFilePathForKey(record.PersonName.Trim()(0))
        Dim records = ReadAllRecordsFromFile(filePath)

        records.Add(CloneRecord(record))
        WriteAllRecordsToFile(filePath, records)

        Return True
    End Function

    Public Function DeleteFirstByNamePrefix(search As String) As Boolean
        If String.IsNullOrWhiteSpace(search) Then Return False

        Dim normalized = search.Trim().ToUpperInvariant()
        Dim filePath = GetFilePathForKey(normalized(0))

        If Not File.Exists(filePath) Then Return False

        Dim records = ReadAllRecordsFromFile(filePath)
        Dim removed As Boolean = False

        For i As Integer = 0 To records.Count - 1
            Dim r = records(i)
            If r Is Nothing Then Continue For
            If String.IsNullOrWhiteSpace(r.PersonName) Then Continue For

            If r.PersonName.Trim().ToUpperInvariant().StartsWith(normalized, StringComparison.Ordinal) Then
                records.RemoveAt(i)
                removed = True
                Exit For
            End If
        Next

        If removed Then
            WriteAllRecordsToFile(filePath, records)
        End If

        Return removed
    End Function

    Public Function UpdateFirstByNamePrefix(search As String, updatedRecord As RolodexPersonRecord) As Boolean
        If String.IsNullOrWhiteSpace(search) Then Return False
        If updatedRecord Is Nothing OrElse String.IsNullOrWhiteSpace(updatedRecord.PersonName) Then Return False

        Dim normalized = search.Trim().ToUpperInvariant()
        Dim sourcePath = GetFilePathForKey(normalized(0))

        If Not File.Exists(sourcePath) Then Return False

        Dim sourceRecords = ReadAllRecordsFromFile(sourcePath)
        Dim matchIndex As Integer = -1

        For i As Integer = 0 To sourceRecords.Count - 1
            Dim r = sourceRecords(i)
            If r Is Nothing Then Continue For
            If String.IsNullOrWhiteSpace(r.PersonName) Then Continue For

            If r.PersonName.Trim().ToUpperInvariant().StartsWith(normalized, StringComparison.Ordinal) Then
                matchIndex = i
                Exit For
            End If
        Next

        If matchIndex = -1 Then Return False

        sourceRecords.RemoveAt(matchIndex)
        WriteAllRecordsToFile(sourcePath, sourceRecords)

        Dim targetPath = GetFilePathForKey(updatedRecord.PersonName.Trim()(0))
        Dim targetRecords = ReadAllRecordsFromFile(targetPath)
        targetRecords.Add(CloneRecord(updatedRecord))
        WriteAllRecordsToFile(targetPath, targetRecords)

        Return True
    End Function

    Public Function ReadAllRecordsForLetter(ch As Char) As List(Of RolodexPersonRecord)
        Dim filePath = GetFilePathForKey(ch)
        Return ReadAllRecordsFromFile(filePath)
    End Function

    Private Shared Sub EnsureBaseFolder()
        If Not Directory.Exists(BaseFolder) Then
            Directory.CreateDirectory(BaseFolder)
        End If
    End Sub

    Private Shared Function GetFilePathForKey(ch As Char) As String
        EnsureBaseFolder()

        Dim key As Char = Char.ToUpperInvariant(ch)

        If Not Char.IsLetterOrDigit(key) Then
            key = "0"c
        End If

        Return Path.Combine(BaseFolder, key & ".LST")
    End Function

    Private Function GetAllShardKeys() As IEnumerable(Of Char)
        Dim keys As New List(Of Char)

        For i As Integer = AscW("0"c) To AscW("9"c)
            keys.Add(ChrW(i))
        Next

        For i As Integer = AscW("A"c) To AscW("Z"c)
            keys.Add(ChrW(i))
        Next

        Return keys
    End Function

    Private Function ReadAllRecordsFromFile(filePath As String) As List(Of RolodexPersonRecord)
        Dim results As New List(Of RolodexPersonRecord)

        If Not File.Exists(filePath) Then
            Return results
        End If

        For Each rawLine In File.ReadAllLines(filePath)
            Dim record = ParseLine(rawLine)
            If record IsNot Nothing Then
                results.Add(record)
            End If
        Next

        Return results
    End Function

    Private Sub WriteAllRecordsToFile(filePath As String, records As List(Of RolodexPersonRecord))
        EnsureBaseFolder()

        Using sw As New StreamWriter(filePath, False)
            For Each record In records
                If record Is Nothing Then Continue For
                If String.IsNullOrWhiteSpace(record.PersonName) Then Continue For

                sw.WriteLine(SerializeRecord(record))
            Next
        End Using
    End Sub

    Private Function ParseLine(line As String) As RolodexPersonRecord
        Try
            If line Is Nothing Then Return Nothing

            Dim cleaned = line.Replace(ControlChars.NullChar, "")
            cleaned = cleaned.Trim()

            If cleaned.Length = 0 Then Return Nothing

            cleaned = cleaned.TrimStart(ChrW(26), " "c, ControlChars.Tab)

            If cleaned.Length = 0 Then Return Nothing

            Using parser As New TextFieldParser(New StringReader(cleaned))
                parser.SetDelimiters(",")
                parser.HasFieldsEnclosedInQuotes = True
                parser.TrimWhiteSpace = False

                Dim fields = parser.ReadFields()
                If fields Is Nothing OrElse fields.Length < 8 Then Return Nothing

                Dim zipValue As Integer = 0
                Dim areaValue As Integer = 0
                Dim phoneValue As Long = 0

                Integer.TryParse(OnlyDigits(fields(4)), zipValue)
                Integer.TryParse(OnlyDigits(fields(5)), areaValue)
                Long.TryParse(OnlyDigits(fields(6)), phoneValue)

                Return New RolodexPersonRecord With {
                    .PersonName = SafeTrim(fields(0)),
                    .Street = SafeTrim(fields(1)),
                    .City = SafeTrim(fields(2)),
                    .StateCode = SafeTrim(fields(3)),
                    .ZipCode = If(zipValue = 0, "", zipValue.ToString(CultureInfo.InvariantCulture)),
                    .AreaCode = If(areaValue = 0, "", areaValue.ToString(CultureInfo.InvariantCulture)),
                    .PhoneNumber = If(phoneValue = 0, "", phoneValue.ToString(CultureInfo.InvariantCulture)),
                    .Misc = SafeTrim(fields(7))
                }
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Private Function SerializeRecord(record As RolodexPersonRecord) As String
        Dim zipValue As Integer = 0
        Dim areaValue As Integer = 0
        Dim phoneValue As Long = 0

        Integer.TryParse(OnlyDigits(record.ZipCode), zipValue)
        Integer.TryParse(OnlyDigits(record.AreaCode), areaValue)
        Long.TryParse(OnlyDigits(record.PhoneNumber), phoneValue)

        Return String.Join(",",
                           Quote(record.PersonName),
                           Quote(record.Street),
                           Quote(record.City),
                           Quote(record.StateCode),
                           zipValue.ToString(CultureInfo.InvariantCulture),
                           areaValue.ToString(CultureInfo.InvariantCulture),
                           phoneValue.ToString(CultureInfo.InvariantCulture),
                           Quote(record.Misc))
    End Function

    Private Function Quote(value As String) As String
        If value Is Nothing Then value = ""
        Return """" & value.Replace("""", """""") & """"
    End Function

    Private Function OnlyDigits(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""
        Return New String(value.Where(Function(c) Char.IsDigit(c)).ToArray())
    End Function

    Private Function SafeTrim(value As String) As String
        If value Is Nothing Then Return ""
        Return value.Trim()
    End Function

    Private Function RecordContains(record As RolodexPersonRecord, term As String) As Boolean
        Dim fields = {
            record.PersonName,
            record.Street,
            record.City,
            record.StateCode,
            record.ZipCode,
            record.AreaCode,
            record.PhoneNumber
        }

        For Each field In fields
            If String.IsNullOrWhiteSpace(field) Then Continue For
            If field.ToUpperInvariant().Contains(term) Then Return True
        Next

        Return False
    End Function

    Private Function CloneRecord(record As RolodexPersonRecord) As RolodexPersonRecord
        If record Is Nothing Then Return Nothing

        Return New RolodexPersonRecord With {
            .PersonName = record.PersonName,
            .Street = record.Street,
            .City = record.City,
            .StateCode = record.StateCode,
            .ZipCode = record.ZipCode,
            .AreaCode = record.AreaCode,
            .PhoneNumber = record.PhoneNumber,
            .Misc = record.Misc,
            .IsCustomer = record.IsCustomer
        }
    End Function

End Class