Option Strict Off
Option Explicit On

Imports System.IO

Public Module AppPaths
    Public ReadOnly Property AppRoot As String = "\\invoice\MainMenu"
    Public ReadOnly Property DataDir As String = Path.Combine(AppRoot, "Data")
    Public ReadOnly Property BackupDir As String = Path.Combine(AppRoot, "Backups")
    Public ReadOnly Property WordDocsDir As String = "\\invoice\word"
End Module