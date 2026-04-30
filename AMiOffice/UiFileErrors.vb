Option Strict On
Option Explicit On

Public Module UiFileErrors

    Public Sub ShowMissingRequiredFile(owner As IWin32Window, screenTitle As String, path As String)
        MessageBox.Show(owner,
                        "Required data file was not found:" & Environment.NewLine &
                        path,
                        screenTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
    End Sub

    Public Sub ShowUnableToReadRequiredFile(owner As IWin32Window, screenTitle As String, path As String, ex As Exception)
        MessageBox.Show(owner,
                        "Unable to read required data file:" & Environment.NewLine &
                        path & Environment.NewLine & Environment.NewLine &
                        ex.Message,
                        screenTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
    End Sub

End Module