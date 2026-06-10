Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' DOS-style personal backup for Option Y.
''' Replicates MAINM.ASC lines 2400-2620 behavior:
''' - Copies all important data files to a backup location
''' - Shows progress and file counts
''' - Modernizes from D: drive to user-selected backup path
''' </summary>
Public Class FormPersonalBackup
    Inherits Form

    Private lblTitle As Label
    Private lblInstructions As Label
    Private txtProgress As TextBox
    Private btnStart As Button
    Private btnCancel As Button
    Private WithEvents bgWorker As System.ComponentModel.BackgroundWorker

    Private _backupPath As String = ""
    Private _totalFiles As Integer = 0
    Private _copiedFiles As Integer = 0
    Private _errors As New List(Of String)()
    Private _cancelled As Boolean = False

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "Ed Dean's Personal Backup"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(800, 600)

        ' Title
        lblTitle = New Label() With {
            .Text = "<<< ED DEAN'S PERSONAL BACKUP >>>",
            .Location = New Point(20, 20),
            .Size = New Size(740, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Instructions
        lblInstructions = New Label() With {
            .Text = "The computer is preparing to backup all the important data." & Environment.NewLine &
                   "This process may take several minutes depending on data size.",
            .Location = New Point(20, 60),
            .Size = New Size(740, 60),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        }

        ' Progress display
        txtProgress = New TextBox() With {
            .Location = New Point(20, 130),
            .Size = New Size(740, 360),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.Black,
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Start button
        btnStart = New Button() With {
            .Text = "Start Backup",
            .Location = New Point(500, 510),
            .Size = New Size(120, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGreen,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold),
            .TabIndex = 0
        }

        ' Cancel button
        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Point(640, 510),
            .Size = New Size(120, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold),
            .TabIndex = 1,
            .DialogResult = DialogResult.Cancel
        }

        ' Background worker for async backup
        bgWorker = New System.ComponentModel.BackgroundWorker() With {
            .WorkerReportsProgress = True,
            .WorkerSupportsCancellation = True
        }

        ' Add controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblInstructions)
        Me.Controls.Add(txtProgress)
        Me.Controls.Add(btnStart)
        Me.Controls.Add(btnCancel)

        Me.CancelButton = btnCancel
        Me.AcceptButton = btnStart

        AddHandler btnStart.Click, AddressOf btnStart_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeDosStyle()
        ' Use configured UNC path (modernized from DOS D: drive)
        _backupPath = LegacyDataPaths.PersonalBackupPath

        lblInstructions.Text = "Ready to backup to:" & Environment.NewLine & _backupPath & Environment.NewLine & Environment.NewLine &
                               "This will copy all important data files to Ed Dean's backup location."
        txtProgress.AppendText("Backup destination: " & _backupPath & Environment.NewLine)
        txtProgress.AppendText(Environment.NewLine)

        ' Verify the backup path is accessible
        Try
            If Not Directory.Exists(_backupPath) Then
                txtProgress.AppendText("WARNING: Backup path does not exist or is not accessible." & Environment.NewLine)
                txtProgress.AppendText("         The backup will attempt to create it." & Environment.NewLine)
                txtProgress.AppendText(Environment.NewLine)
            End If
        Catch ex As Exception
            txtProgress.AppendText("WARNING: Cannot access backup path: " & ex.Message & Environment.NewLine)
            txtProgress.AppendText(Environment.NewLine)
        End Try

        txtProgress.AppendText("Click 'Start Backup' to begin..." & Environment.NewLine)
    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        If bgWorker.IsBusy Then
            Return
        End If

        ' Clear previous run
        txtProgress.Clear()
        _totalFiles = 0
        _copiedFiles = 0
        _errors.Clear()
        _cancelled = False

        ' Disable start, enable cancel
        btnStart.Enabled = False
        btnCancel.Text = "Cancel"
        btnCancel.Enabled = True

        txtProgress.AppendText("Starting backup..." & Environment.NewLine)
        txtProgress.AppendText(Environment.NewLine)

        bgWorker.RunWorkerAsync()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        If bgWorker.IsBusy Then
            bgWorker.CancelAsync()
            _cancelled = True
            txtProgress.AppendText(Environment.NewLine)
            txtProgress.AppendText("Cancelling backup..." & Environment.NewLine)
        Else
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub bgWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles bgWorker.DoWork
        Dim worker As System.ComponentModel.BackgroundWorker = DirectCast(sender, System.ComponentModel.BackgroundWorker)
        Dim baseDir As String = LegacyDataPaths.BaseDataDir

        ' DOS backup list from MAINM.ASC lines 2430-2538
        ' Uses XCOPY to D: drive, modernized to UNC path with File.Copy
        Dim backupSets As New List(Of BackupSet)()

        ' Line 2440: *.DAT files
        backupSets.Add(New BackupSet("*.DAT", baseDir, _backupPath, "Data files"))

        ' Line 2450: *.CUR files
        backupSets.Add(New BackupSet("*.CUR", baseDir, _backupPath, "Current ledger files"))

        ' Line 2460: *.TOT files
        backupSets.Add(New BackupSet("*.TOT", baseDir, _backupPath, "Totals files"))

        ' Line 2470: *.INV files
        backupSets.Add(New BackupSet("*.INV", baseDir, _backupPath, "Invoice files"))

        ' Line 2480: *.DEP files
        backupSets.Add(New BackupSet("*.DEP", baseDir, _backupPath, "Deposit files"))

        ' Line 2490: *.CHK files
        backupSets.Add(New BackupSet("*.CHK", baseDir, _backupPath, "Check files"))

        ' Line 2500: *.BAS files
        backupSets.Add(New BackupSet("*.BAS", baseDir, _backupPath, "BASIC program files"))

        ' Line 2501: *.PMS files
        backupSets.Add(New BackupSet("*.PMS", baseDir, _backupPath, "PMS files"))

        ' Line 2510: BILLS.* files
        backupSets.Add(New BackupSet("BILLS.*", baseDir, _backupPath, "Bills files"))

        ' Line 2520: Current year files (*.XX where XX = current 2-digit year)
        Dim yearExt As String = DateTime.Now.ToString("yy")
        backupSets.Add(New BackupSet("*." & yearExt, baseDir, _backupPath, "Current year files (*." & yearExt & ")"))

        ' Line 2512: Word processor docs
        Dim wordDir As String = LegacyDataPaths.WordDocDir
        If Directory.Exists(wordDir) Then
            Dim wordBackup As String = Path.Combine(_backupPath, "Word")
            backupSets.Add(New BackupSet("*.DOC", wordDir, wordBackup, "Word processor documents"))
        End If

        ' Lines 2522-2525: Phone directory files (if they exist)
        Dim phoneDir As String = Path.Combine(baseDir, "Phone")
        If Directory.Exists(phoneDir) Then
            Dim phoneBackup As String = Path.Combine(_backupPath, "Phone")
            backupSets.Add(New BackupSet("*.BAS", phoneDir, phoneBackup, "Phone directory BAS files"))
            backupSets.Add(New BackupSet("*.LST", phoneDir, phoneBackup, "Phone directory LST files"))
            backupSets.Add(New BackupSet("*.EXE", phoneDir, phoneBackup, "Phone directory EXE files"))
            backupSets.Add(New BackupSet("*.OVL", phoneDir, phoneBackup, "Phone directory OVL files"))
        End If

        ' Lines 2530-2538: Shop card files (if they exist)
        Dim shopCardBaseDir As String = Path.Combine(baseDir, "ShopCard")
        If Directory.Exists(shopCardBaseDir) Then
            Dim shopCardBackup As String = Path.Combine(_backupPath, "ShopCard")
            For i As Integer = 0 To 8
                Dim shopCardSubDir As String = Path.Combine(shopCardBaseDir, i.ToString())
                If Directory.Exists(shopCardSubDir) Then
                    Dim shopCardSubBackup As String = Path.Combine(shopCardBackup, i.ToString())
                    backupSets.Add(New BackupSet("*.CRD", shopCardSubDir, shopCardSubBackup, "Shop card files (subdirectory " & i.ToString() & ")"))
                End If
            Next
        End If

        ' LOGBOOK files (not in DOS list but critical)
        backupSets.Add(New BackupSet("LOGBOOK.*", baseDir, _backupPath, "Log book files"))

        ' Execute backup
        For Each backupSet As BackupSet In backupSets
            If worker.CancellationPending Then
                e.Cancel = True
                Return
            End If

            PerformBackup(worker, backupSet)
        Next

    End Sub

    Private Sub PerformBackup(worker As System.ComponentModel.BackgroundWorker, backupSet As BackupSet)
        Try
            ' Report start
            worker.ReportProgress(0, "Backing up " & backupSet.Description & "...")

            If Not Directory.Exists(backupSet.SourceDir) Then
                worker.ReportProgress(0, "  Source not found: " & backupSet.SourceDir)
                Return
            End If

            ' Create destination if needed
            If Not Directory.Exists(backupSet.DestDir) Then
                Directory.CreateDirectory(backupSet.DestDir)
            End If

            ' Find matching files
            Dim files() As String = Directory.GetFiles(backupSet.SourceDir, backupSet.Pattern)
            _totalFiles += files.Length

            If files.Length = 0 Then
                worker.ReportProgress(0, "  No files found matching: " & backupSet.Pattern)
                Return
            End If

            worker.ReportProgress(0, "  Found " & files.Length.ToString() & " file(s)")

            ' Copy files with progress updates every 10 files (much faster)
            Dim copyCount As Integer = 0
            For Each sourceFile As String In files
                If worker.CancellationPending Then
                    Return
                End If

                Try
                    Dim fileName As String = Path.GetFileName(sourceFile)
                    Dim destFile As String = Path.Combine(backupSet.DestDir, fileName)

                    ' Use buffered copy (faster for network drives)
                    Using sourceStream As New FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize:=81920)
                        Using destStream As New FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize:=81920)
                            sourceStream.CopyTo(destStream, bufferSize:=81920)
                        End Using
                    End Using

                    _copiedFiles += 1
                    copyCount += 1

                    ' Only report progress every 10 files (reduces UI overhead)
                    If copyCount Mod 10 = 0 Then
                        worker.ReportProgress(0, "  Copied " & copyCount.ToString() & " / " & files.Length.ToString() & " files...")
                    End If

                Catch ex As Exception
                    _errors.Add("Failed to copy " & Path.GetFileName(sourceFile) & ": " & ex.Message)
                    worker.ReportProgress(0, "  ERROR: " & Path.GetFileName(sourceFile))
                End Try
            Next

            ' Final count
            worker.ReportProgress(0, "  Completed: " & copyCount.ToString() & " file(s)")

        Catch ex As Exception
            _errors.Add("Backup set error (" & backupSet.Description & "): " & ex.Message)
            worker.ReportProgress(0, "  ERROR: " & ex.Message)
        End Try
    End Sub

    Private Sub bgWorker_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
        If e.UserState IsNot Nothing Then
            txtProgress.AppendText(e.UserState.ToString() & Environment.NewLine)
            txtProgress.SelectionStart = txtProgress.Text.Length
            txtProgress.ScrollToCaret()
        End If
    End Sub

    Private Sub bgWorker_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bgWorker.RunWorkerCompleted
        btnStart.Enabled = True
        btnCancel.Text = "Close"
        btnCancel.Enabled = True

        txtProgress.AppendText(Environment.NewLine)
        txtProgress.AppendText("----------------------------------------" & Environment.NewLine)

        If e.Cancelled Then
            txtProgress.AppendText("BACKUP CANCELLED" & Environment.NewLine)
        ElseIf e.Error IsNot Nothing Then
            txtProgress.AppendText("BACKUP FAILED: " & e.Error.Message & Environment.NewLine)
        Else
            txtProgress.AppendText("BACKUP COMPLETE!" & Environment.NewLine)
            txtProgress.AppendText("Files copied: " & _copiedFiles.ToString() & " / " & _totalFiles.ToString() & Environment.NewLine)

            If _errors.Count > 0 Then
                txtProgress.AppendText(Environment.NewLine)
                txtProgress.AppendText("Errors (" & _errors.Count.ToString() & "):" & Environment.NewLine)
                For Each err As String In _errors
                    txtProgress.AppendText("  " & err & Environment.NewLine)
                Next
            End If

            txtProgress.AppendText(Environment.NewLine)
            txtProgress.AppendText("Backup location: " & _backupPath & Environment.NewLine)
        End If

        txtProgress.SelectionStart = txtProgress.Text.Length
        txtProgress.ScrollToCaret()
    End Sub

    Private Class BackupSet
        Public Property Pattern As String
        Public Property SourceDir As String
        Public Property DestDir As String
        Public Property Description As String

        Public Sub New(pattern As String, sourceDir As String, destDir As String, description As String)
            Me.Pattern = pattern
            Me.SourceDir = sourceDir
            Me.DestDir = destDir
            Me.Description = description
        End Sub
    End Class
End Class
