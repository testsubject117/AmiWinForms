Option Strict Off
Option Explicit On

Imports System.IO

Public Module MigrationService

    Private ReadOnly LegacyRoot As String = "\\invoice"
    Private ReadOnly MarkerPath As String = Path.Combine(AppPaths.DataDir, ".migrated")

    Public Sub EnsureFoldersAndMigrateOnce()
        Directory.CreateDirectory(AppPaths.AppRoot)
        Directory.CreateDirectory(AppPaths.DataDir)
        Directory.CreateDirectory(AppPaths.BackupDir)

        ' If we've already migrated on this share, do nothing.
        If File.Exists(MarkerPath) Then Return

        Dim patterns As String() = {
            "*.DAT", "*.CUR", "*.LST", "*.PRC",
            "PACK.NUM", "BILLS.*", "*.CHK", "*.TXT"
        }

        Dim movedAny As Boolean = False

        For Each pattern In patterns
            Dim files As String()

            Try
                files = Directory.GetFiles(LegacyRoot, pattern, SearchOption.TopDirectoryOnly)
            Catch
                Continue For
            End Try

            For Each src In files
                Dim dest As String = Path.Combine(AppPaths.DataDir, Path.GetFileName(src))

                ' Skip if already migrated
                If File.Exists(dest) Then Continue For

                Try
                    File.Move(src, dest)
                    movedAny = True
                Catch
                    ' ignore (in use / permissions / etc.)
                End Try
            Next
        Next

        ' Create marker even if nothing moved, so we don't keep scanning \\invoice\ forever.
        Try
            File.WriteAllText(MarkerPath, $"migrated={DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        Catch
            ' If we can't write marker, migration will try again next time.
        End Try
    End Sub

End Module