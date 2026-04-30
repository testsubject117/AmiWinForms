Option Strict Off
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Linq

Public Module BackupService

    Public Sub RunBackupPriceListAndRolodex()
        Directory.CreateDirectory(AppPaths.DataDir)
        Directory.CreateDirectory(AppPaths.BackupDir)

        ' 1) Word docs => WORD_yyyy-MM-dd_HHmmss.zip (timestamped)
        Dim stamp As String = DateTime.Now.ToString("yyyy-MM-dd_HHmmss")
        Dim wordZip As String = Path.Combine(AppPaths.BackupDir, $"WORD_{stamp}.zip")
        CreateZipFromPattern(wordZip, AppPaths.WordDocsDir, "*.doc")

        ' 2) Rotating data backup => BACKUP##.zip (LOCKED)
        Dim lockPath As String = Path.Combine(AppPaths.DataDir, "backup.lock")

        Using lockStream As FileStream = AcquireBackupLock(lockPath)

            Dim packNumPath As String = Path.Combine(AppPaths.DataDir, "PACK.NUM")
            Dim n As Integer = GetNextPackNumberAndAdvance(packNumPath)
            Dim dataZip As String = Path.Combine(AppPaths.BackupDir, $"BACKUP{n}.zip")

            CreateDataZip(dataZip, AppPaths.DataDir)

        End Using
    End Sub

    Private Sub CreateZipFromPattern(zipPath As String, folder As String, pattern As String)
        Using fs As New FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)
            Using archive As New ZipArchive(fs, ZipArchiveMode.Create)
                If Directory.Exists(folder) Then
                    For Each f In Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly)
                        archive.CreateEntryFromFile(f, Path.GetFileName(f), CompressionLevel.Optimal)
                    Next
                End If
            End Using
        End Using
    End Sub

    Private Sub CreateDataZip(zipPath As String, dataDir As String)
        Dim includePatterns As String() = {
            "*.DAT", "*.CUR", "PRC*.PRC", "PHONE*.LST", "BILLS.*", "PACK.NUM"
        }

        Dim files As New System.Collections.Generic.List(Of String)

        For Each pat In includePatterns
            If Directory.Exists(dataDir) Then
                Try
                    files.AddRange(Directory.GetFiles(dataDir, pat, SearchOption.TopDirectoryOnly))
                Catch
                End Try
            End If
        Next

        files = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList()

        Using fs As New FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)
            Using archive As New ZipArchive(fs, ZipArchiveMode.Create)
                For Each f In files
                    archive.CreateEntryFromFile(f, Path.GetFileName(f), CompressionLevel.Optimal)
                Next
            End Using
        End Using
    End Sub

    Private Function GetNextPackNumberAndAdvance(packNumPath As String) As Integer
        Dim n As Integer = 1

        Try
            If File.Exists(packNumPath) Then
                Integer.TryParse(File.ReadAllText(packNumPath).Trim(), n)
            End If
        Catch
            n = 1
        End Try

        If n < 1 OrElse n > 99 Then n = 1

        Dim nextN As Integer = n + 1
        If nextN > 99 Then nextN = 1

        Try
            File.WriteAllText(packNumPath, nextN.ToString())
        Catch
        End Try

        Return n
    End Function
    Private Function AcquireBackupLock(lockPath As String) As FileStream
        ' Creating with FileShare.None makes it a cross-machine mutex on a UNC share.
        ' If another PC holds it, this throws an IOException.
        Return New FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
    End Function

End Module