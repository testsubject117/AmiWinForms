Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms

''' <summary>
''' Option T - Change Date or Time
''' DOS equivalent: Lines 1670-1700 in MAINM.ASC
''' Simple form to display current date/time with warning about permanence
''' </summary>
Public Class FormDateTimeChange
    Inherits Form

    Private lblTitle As Label
    Private lblCurrentDateTime As Label
    Private lblWarning As Label
    Private btnOk As Button

    Public Sub New()
        InitializeComponent()
        Me.KeyPreview = True
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form setup
        Me.Text = "Change Date or Time"
        Me.ClientSize = New Drawing.Size(600, 300)
        Me.BackColor = Drawing.Color.Black
        Me.ForeColor = Drawing.Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Drawing.Font("Consolas", 12.0F, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Point)

        ' Title label
        lblTitle = New Label() With {
            .Text = "<<< CHANGE DATE OR TIME >>>",
            .Location = New Drawing.Point(20, 20),
            .Size = New Drawing.Size(560, 30),
            .ForeColor = Drawing.Color.Yellow,
            .Font = New Drawing.Font("Consolas", 14.0F, Drawing.FontStyle.Bold),
            .TextAlign = Drawing.ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblTitle)

        ' Current date/time label
        lblCurrentDateTime = New Label() With {
            .Location = New Drawing.Point(40, 80),
            .Size = New Drawing.Size(520, 60),
            .ForeColor = Drawing.Color.White,
            .Font = New Drawing.Font("Consolas", 12.0F, Drawing.FontStyle.Regular),
            .TextAlign = Drawing.ContentAlignment.TopLeft
        }
        Me.Controls.Add(lblCurrentDateTime)

        ' Warning label
        lblWarning = New Label() With {
            .Text = "This change will NOT be permanent!" & Environment.NewLine & Environment.NewLine &
                    "Windows manages the system date and time." & Environment.NewLine &
                    "To make permanent changes, use Windows Settings:" & Environment.NewLine &
                    "Settings > Time & Language > Date & Time",
            .Location = New Drawing.Point(40, 160),
            .Size = New Drawing.Size(520, 90),
            .ForeColor = Drawing.Color.Cyan,
            .Font = New Drawing.Font("Consolas", 10.0F, Drawing.FontStyle.Regular),
            .TextAlign = Drawing.ContentAlignment.TopLeft
        }
        Me.Controls.Add(lblWarning)

        ' OK button
        btnOk = New Button() With {
            .Text = "OK",
            .Location = New Drawing.Point(250, 260),
            .Size = New Drawing.Size(100, 30),
            .BackColor = Drawing.Color.FromArgb(64, 64, 64),
            .ForeColor = Drawing.Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Drawing.Font("Consolas", 11.0F, Drawing.FontStyle.Bold)
        }
        btnOk.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnOk.Click, AddressOf btnOk_Click
        Me.Controls.Add(btnOk)

        Me.AcceptButton = btnOk
        Me.CancelButton = btnOk

        Me.ResumeLayout(False)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        UpdateDateTime()

        ' Start timer to update display every second
        Dim tmr As New Timer() With {.Interval = 1000}
        AddHandler tmr.Tick, Sub()
                                 UpdateDateTime()
                             End Sub
        tmr.Start()
    End Sub

    Private Sub UpdateDateTime()
        Dim now As DateTime = DateTime.Now
        lblCurrentDateTime.Text =
            "Current Date: " & now.ToString("MM-dd-yyyy") & "  (" & now.DayOfWeek.ToString() & ")" & Environment.NewLine &
            "Current Time: " & now.ToString("hh:mm:ss tt")
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class

