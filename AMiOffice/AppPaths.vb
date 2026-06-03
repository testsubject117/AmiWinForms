Option Strict Off
Option Explicit On

Imports System.IO

Public Module AppPaths
    Public ReadOnly Property AppRoot As String = "\\invoice\MainMenu"
    Public ReadOnly Property DataDir As String = Path.Combine(AppRoot, "Data")
    Public ReadOnly Property BackupDir As String = Path.Combine(AppRoot, "Backups")

    ' New desired location (matches the "MainMenu\Data\..." layout).
    Public ReadOnly Property WordDocsDirNew As String = Path.Combine(DataDir, "Word")

    ' Legacy location (what the app used previously).
    Public ReadOnly Property WordDocsDirLegacy As String = "\\invoice\word"

    ' Backward-compatible effective location:
    ' - Prefer new location if it exists.
    ' - Otherwise fall back to legacy.
    Public ReadOnly Property WordDocsDir As String
        Get
            Try
                If Directory.Exists(WordDocsDirNew) Then Return WordDocsDirNew
            Catch
                ' ignore network/permission issues and fall back
            End Try

            Return WordDocsDirLegacy
        End Get
    End Property
End Module