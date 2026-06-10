Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' DOS-style spec index copy for Option O.
''' Replicates MAINM.ASC lines 1590-1595 behavior:
''' - Prompts user to select source folder (modernized from floppy A:)
''' - Copies all *.DOC files to word processor directory
''' - Shows progress and completion message
''' </summary>
Public Class FormCopySpecIndex
    Inherits Form

    Private lblTitle As Label
    Private lblInstructions As Label
    Private txtProgress As TextBox
    Private btnSelectSource As Button
    Private btnClose As Button

    Private _sourcePath As String = ""
    Private _destPath As String = ""

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "Copy Spec Index"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(700, 500)

        ' Title
        lblTitle = New Label() With {
            .Text = "<<< COPY SPEC INDEX >>>",
            .Location = New Point(20, 20),
            .Size = New Size(640, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Instructions
        lblInstructions = New Label() With {
            .Text = "Select the folder containing spec index documents (*.DOC files)." & Environment.NewLine &
                   "DOS version: ""Put in floppy with spec index & hit [enter]""" & Environment.NewLine & Environment.NewLine &
                   "Files will be copied to: " & LegacyDataPaths.WordDocDir,
            .Location = New Point(20, 60),
            .Size = New Size(640, 80),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        }

        ' Progress display
        txtProgress = New TextBox() With {
            .Location = New Point(20, 150),
            .Size = New Size(640, 250),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.Black,
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Select source button
        btnSelectSource = New Button() With {
            .Text = "Select Source Folder",
            .Location = New Point(400, 420),
            .Size = New Size(140, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGreen,
            .Font = New Font("Consolas", 9.0F, FontStyle.Bold),
            .TabIndex = 0
        }

        ' Close button
        btnClose = New Button() With {
            .Text = "Close",
            .Location = New Point(550, 420),
            .Size = New Size(110, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 9.0F, FontStyle.Bold),
            .TabIndex = 1,
            .DialogResult = DialogResult.Cancel
        }

        ' Add controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblInstructions)
        Me.Controls.Add(txtProgress)
        Me.Controls.Add(btnSelectSource)
        Me.Controls.Add(btnClose)

        Me.CancelButton = btnClose
        Me.AcceptButton = btnSelectSource

        AddHandler btnSelectSource.Click, AddressOf btnSelectSource_Click
        AddHandler btnClose.Click, AddressOf btnClose_Click

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeDosStyle()
        _destPath = LegacyDataPaths.WordDocDir
        txtProgress.AppendText("Ready to copy spec index documents." & Environment.NewLine)
        txtProgress.AppendText(Environment.NewLine)
        txtProgress.AppendText("Destination: " & _destPath & Environment.NewLine)
        txtProgress.AppendText(Environment.NewLine)
        txtProgress.AppendText("Click 'Select Source Folder' to begin..." & Environment.NewLine)
    End Sub

    Private Sub btnSelectSource_Click(sender As Object, e As EventArgs)
        Using folderDialog As New FolderBrowserDialog()
            folderDialog.Description = "Select folder containing spec index *.DOC files (DOS: A:\DOC\):"
            folderDialog.ShowNewFolderButton = False

            If folderDialog.ShowDialog() = DialogResult.OK Then
                _sourcePath = folderDialog.SelectedPath
                PerformCopy()
            End If
        End Using
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub PerformCopy()
        Try
            txtProgress.Clear()
            txtProgress.AppendText("Copying spec index files..." & Environment.NewLine)
            txtProgress.AppendText(Environment.NewLine)
            txtProgress.AppendText("Source: " & _sourcePath & Environment.NewLine)
            txtProgress.AppendText("Destination: " & _destPath & Environment.NewLine)
            txtProgress.AppendText(Environment.NewLine)

            ' Check source exists
            If Not Directory.Exists(_sourcePath) Then
                txtProgress.AppendText("ERROR: Source folder not found." & Environment.NewLine)
                Return
            End If

            ' Check destination exists
            If Not Directory.Exists(_destPath) Then
                txtProgress.AppendText("ERROR: Destination folder not found: " & _destPath & Environment.NewLine)
                Return
            End If

            ' Find *.DOC files
            Dim docFiles() As String = Directory.GetFiles(_sourcePath, "*.DOC", SearchOption.TopDirectoryOnly)

            If docFiles.Length = 0 Then
                txtProgress.AppendText("No *.DOC files found in source folder." & Environment.NewLine)
                Return
            End If

            txtProgress.AppendText("Found " & docFiles.Length.ToString() & " file(s) to copy." & Environment.NewLine)
            txtProgress.AppendText(Environment.NewLine)

            ' Copy files
            Dim copiedCount As Integer = 0
            Dim errorCount As Integer = 0

            For Each sourceFile As String In docFiles
                Try
                    Dim fileName As String = Path.GetFileName(sourceFile)
                    Dim destFile As String = Path.Combine(_destPath, fileName)

                    ' Use buffered copy for speed
                    Using sourceStream As New FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize:=81920)
                        Using destStream As New FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize:=81920)
                            sourceStream.CopyTo(destStream, bufferSize:=81920)
                        End Using
                    End Using

                    copiedCount += 1
                    txtProgress.AppendText("Copied: " & fileName & Environment.NewLine)

                Catch ex As Exception
                    errorCount += 1
                    txtProgress.AppendText("ERROR: " & Path.GetFileName(sourceFile) & " - " & ex.Message & Environment.NewLine)
                End Try
            Next

            ' Summary (DOS: "DONE.")
            txtProgress.AppendText(Environment.NewLine)
            txtProgress.AppendText("========================================" & Environment.NewLine)
            txtProgress.AppendText("DONE." & Environment.NewLine)
            txtProgress.AppendText("Copied: " & copiedCount.ToString() & " file(s)" & Environment.NewLine)

            If errorCount > 0 Then
                txtProgress.AppendText("Errors: " & errorCount.ToString() & " file(s)" & Environment.NewLine)
            End If

            txtProgress.SelectionStart = txtProgress.Text.Length
            txtProgress.ScrollToCaret()

            ' Change button text to indicate completion
            btnSelectSource.Text = "Copy More Files"

        Catch ex As Exception
            txtProgress.AppendText(Environment.NewLine)
            txtProgress.AppendText("ERROR: " & ex.Message & Environment.NewLine)
        End Try
    End Sub
End Class
