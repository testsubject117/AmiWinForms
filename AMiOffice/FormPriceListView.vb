Option Strict Off
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' View procedures for a customer's price list
''' </summary>
Public Class FormPriceListView
    Inherits Form

    Private _customer As String
    Private _dataGridView As DataGridView

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Price List - View Procedures"
        Me.Size = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.Black
        Me.ForeColor = Color.FromArgb(170, 170, 170)
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)

        ' Title label
        Dim lblTitle As New Label()
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 40
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        lblTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        lblTitle.Text = "PRICE LIST"
        Me.Controls.Add(lblTitle)

        ' DataGridView
        _dataGridView = New DataGridView()
        _dataGridView.Dock = DockStyle.Fill
        _dataGridView.ReadOnly = True
        _dataGridView.AllowUserToAddRows = False
        _dataGridView.AllowUserToDeleteRows = False
        _dataGridView.AllowUserToResizeRows = False
        _dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _dataGridView.MultiSelect = False
        _dataGridView.RowHeadersVisible = False
        _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _dataGridView.BackgroundColor = Color.Black
        _dataGridView.ForeColor = Color.FromArgb(170, 170, 170)
        _dataGridView.GridColor = Color.FromArgb(60, 60, 60)
        _dataGridView.DefaultCellStyle.BackColor = Color.Black
        _dataGridView.DefaultCellStyle.ForeColor = Color.FromArgb(170, 170, 170)
        _dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 64, 128)
        _dataGridView.DefaultCellStyle.SelectionForeColor = Color.White
        _dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40)
        _dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(170, 170, 170)
        _dataGridView.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _dataGridView.EnableHeadersVisualStyles = False

        Me.Controls.Add(_dataGridView)

        ' Close button
        Dim btnClose As New Button()
        btnClose.Dock = DockStyle.Bottom
        btnClose.Height = 40
        btnClose.Text = "[Q] Close"
        btnClose.BackColor = Color.FromArgb(40, 40, 40)
        btnClose.ForeColor = Color.FromArgb(170, 170, 170)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        ' ESC/Q to close
        Me.KeyPreview = True
        AddHandler Me.KeyDown, Sub(sender, e)
                                   If e.KeyCode = Keys.Escape OrElse e.KeyCode = Keys.Q Then
                                       Me.Close()
                                   End If
                               End Sub
    End Sub

    Public Sub SetData(customer As String, procedures As List(Of Object))
        _customer = customer
        Me.Text = "Price List - " & customer

        Dim dt As New DataTable()
        dt.Columns.Add("PROCEDURE", GetType(String))
        dt.Columns.Add("EFT DATE", GetType(String))
        dt.Columns.Add("MIN. CHARGE", GetType(String))
        dt.Columns.Add("PRICE", GetType(String))

        For Each proc In procedures
            Dim procType = proc.GetType()
            Dim procName = procType.GetProperty("ProcedureName").GetValue(proc, Nothing).ToString()
            Dim effDate = procType.GetProperty("EffectiveDate").GetValue(proc, Nothing).ToString()
            Dim minCharge = CDec(procType.GetProperty("MinCharge").GetValue(proc, Nothing))
            Dim price = CDec(procType.GetProperty("Price").GetValue(proc, Nothing))
            Dim priceType = procType.GetProperty("PriceType").GetValue(proc, Nothing).ToString()

            dt.Rows.Add(procName, effDate, minCharge.ToString("C"), price.ToString("C") & priceType)
        Next

        _dataGridView.DataSource = dt

        If _dataGridView.Columns.Count >= 4 Then
            _dataGridView.Columns(0).FillWeight = 35
            _dataGridView.Columns(1).FillWeight = 20
            _dataGridView.Columns(2).FillWeight = 20
            _dataGridView.Columns(3).FillWeight = 25
        End If
    End Sub
End Class
