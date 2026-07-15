Option Strict On
Option Explicit On

Imports System.Windows.Forms

Public Module UiFileErrors

    Public Sub ShowMissingRequiredFile(owner As IWin32Window, screenTitle As String, path As String)
        DosMessageBox.Show(owner,
                        "Required data file was not found:" & Environment.NewLine &
                        path,
                        screenTitle,
                        MessageBoxButtons.OK)
    End Sub

    Public Sub ShowUnableToReadRequiredFile(owner As IWin32Window, screenTitle As String, path As String, ex As Exception)
        DosMessageBox.Show(owner,
                        "Unable to read required data file:" & Environment.NewLine &
                        path & Environment.NewLine & Environment.NewLine &
                        ex.Message,
                        screenTitle,
                        MessageBoxButtons.OK)
    End Sub

End Module
