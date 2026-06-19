Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Friend Module UiTheme

    ' --- Core DOS palette / fonts ------------------------------------------------

    Friend ReadOnly Property DosBackColor As Color
        Get
            Return Color.Black
        End Get
    End Property

    Friend ReadOnly Property DosForeColor As Color
        Get
            Return Color.White
        End Get
    End Property

    Friend ReadOnly Property DosAccentColor As Color
        Get
            Return Color.Yellow
        End Get
    End Property

    Friend ReadOnly Property DosDimForeColor As Color
        Get
            Return Color.Gainsboro
        End Get
    End Property

    Friend Function CreateDosFont(Optional size As Single = 12.0F,
                                  Optional style As FontStyle = FontStyle.Regular) As Font
        Return New Font("Consolas", size, style, GraphicsUnit.Point)
    End Function

    ' --- Title styling (existing behavior preserved) ----------------------------

    Friend ReadOnly Property DosTitleForeColor As Color
        Get
            Return DosAccentColor
        End Get
    End Property

    Friend ReadOnly Property DosTitleBackColor As Color
        Get
            Return DosBackColor
        End Get
    End Property

    Friend Function CreateDosTitleFont() As Font
        ' Back to medium size - we'll reduce padding instead
        Return New Font("Castellar", 28.0F, FontStyle.Bold, GraphicsUnit.Point)
    End Function

    Friend Sub ApplyDosTitleStyle(lbl As Label)
        If lbl Is Nothing Then Return

        lbl.ForeColor = DosTitleForeColor
        lbl.BackColor = DosTitleBackColor
        lbl.Font = CreateDosTitleFont()
        lbl.TextAlign = ContentAlignment.MiddleLeft
    End Sub

    ' --- Full-form / recursive theming ------------------------------------------

    ''' <summary>
    ''' Applies a DOS look & feel to a form and all child controls (recursively).
    ''' Use this AFTER dynamically building UI (e.g. after BuildUi()).
    ''' </summary>
    Friend Sub ApplyDosTheme(frm As Form)
        If frm Is Nothing Then Return

        frm.BackColor = DosBackColor
        frm.ForeColor = DosForeColor
        frm.Font = CreateDosFont(12.0F)
        frm.KeyPreview = True

        ApplyDosTheme(DirectCast(frm, Control))
    End Sub

    ''' <summary>
    ''' Applies a DOS look & feel to a control and its children (recursively).
    ''' </summary>
    Friend Sub ApplyDosTheme(root As Control)
        If root Is Nothing Then Return

        ' Root-level defaults (children often inherit, but many controls don't).
        If Not TypeOf root Is Form Then
            SafeSetBackFore(root, DosBackColor, DosForeColor)
            SafeSetFont(root, CreateDosFont(12.0F))
        End If

        ' Control-specific overrides
        If TypeOf root Is Button Then
            ThemeButton(DirectCast(root, Button))
        ElseIf TypeOf root Is TextBox Then
            ThemeTextBox(DirectCast(root, TextBox))
        ElseIf TypeOf root Is RichTextBox Then
            ThemeRichTextBox(DirectCast(root, RichTextBox))
        ElseIf TypeOf root Is ComboBox Then
            ThemeComboBox(DirectCast(root, ComboBox))
        ElseIf TypeOf root Is Label Then
            ThemeLabel(DirectCast(root, Label))
        ElseIf TypeOf root Is ListBox Then
            ThemeListBox(DirectCast(root, ListBox))
        ElseIf TypeOf root Is ListView Then
            ThemeListView(DirectCast(root, ListView))
        ElseIf TypeOf root Is DataGridView Then
            ThemeDataGridView(DirectCast(root, DataGridView))
        ElseIf TypeOf root Is Panel OrElse TypeOf root Is FlowLayoutPanel OrElse TypeOf root Is TableLayoutPanel Then
            ' Containers: ensure background is black
            SafeSetBackFore(root, DosBackColor, DosForeColor)
        End If

        ' Recurse
        For Each child As Control In root.Controls
            ApplyDosTheme(child)
        Next
    End Sub

    ' --- Control themers ---------------------------------------------------------

    Private Sub ThemeButton(btn As Button)
        If btn Is Nothing Then Return

        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.BorderColor = DosForeColor
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 32, 32)
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(48, 48, 48)

        btn.BackColor = DosBackColor
        btn.ForeColor = DosForeColor
        btn.Font = CreateDosFont(12.0F, FontStyle.Regular)

        ' Helps reduce "WinForms-y" focus rectangle feel a bit
        btn.UseVisualStyleBackColor = False
    End Sub

    Private Sub ThemeTextBox(tb As TextBox)
        If tb Is Nothing Then Return

        tb.BackColor = DosBackColor
        tb.ForeColor = DosForeColor
        tb.BorderStyle = BorderStyle.FixedSingle
        tb.Font = CreateDosFont(12.0F)
    End Sub

    Private Sub ThemeRichTextBox(rtb As RichTextBox)
        If rtb Is Nothing Then Return

        rtb.BackColor = DosBackColor
        rtb.ForeColor = DosForeColor
        rtb.BorderStyle = BorderStyle.FixedSingle
        rtb.Font = CreateDosFont(12.0F)
    End Sub

    Private Sub ThemeComboBox(cb As ComboBox)
        If cb Is Nothing Then Return

        cb.BackColor = DosBackColor
        cb.ForeColor = DosForeColor
        cb.Font = CreateDosFont(12.0F)

        ' Note: ComboBox ignores BackColor in some styles; forcing Flat helps.
        cb.FlatStyle = FlatStyle.Flat
    End Sub

    Private Sub ThemeLabel(lbl As Label)
        If lbl Is Nothing Then Return

        lbl.BackColor = DosBackColor
        lbl.ForeColor = DosForeColor
        lbl.Font = CreateDosFont(12.0F)
    End Sub

    Private Sub ThemeListBox(lb As ListBox)
        If lb Is Nothing Then Return

        lb.BackColor = DosBackColor
        lb.ForeColor = DosForeColor
        lb.BorderStyle = BorderStyle.FixedSingle
        lb.Font = CreateDosFont(12.0F)
    End Sub

    Private Sub ThemeListView(lv As ListView)
        If lv Is Nothing Then Return

        lv.BackColor = DosBackColor
        lv.ForeColor = DosForeColor
        lv.Font = CreateDosFont(12.0F)
    End Sub

    Private Sub ThemeDataGridView(grid As DataGridView)
        If grid Is Nothing Then Return

        grid.BackgroundColor = DosBackColor
        grid.GridColor = Color.FromArgb(64, 64, 64)
        grid.BorderStyle = BorderStyle.FixedSingle

        grid.EnableHeadersVisualStyles = False

        grid.DefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = DosBackColor,
            .ForeColor = DosForeColor,
            .SelectionBackColor = Color.FromArgb(64, 64, 64),
            .SelectionForeColor = DosForeColor,
            .Font = CreateDosFont(12.0F)
        }

        grid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = DosBackColor,
            .ForeColor = DosAccentColor,
            .SelectionBackColor = DosBackColor,
            .SelectionForeColor = DosAccentColor,
            .Font = CreateDosFont(12.0F, FontStyle.Bold),
            .Alignment = DataGridViewContentAlignment.MiddleLeft
        }

        grid.RowHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = DosBackColor,
            .ForeColor = DosForeColor,
            .SelectionBackColor = Color.FromArgb(64, 64, 64),
            .SelectionForeColor = DosForeColor,
            .Font = CreateDosFont(12.0F)
        }

        grid.RowsDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = DosBackColor,
            .ForeColor = DosForeColor,
            .SelectionBackColor = Color.FromArgb(64, 64, 64),
            .SelectionForeColor = DosForeColor,
            .Font = CreateDosFont(12.0F)
        }

        grid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = DosBackColor,
            .ForeColor = DosForeColor,
            .SelectionBackColor = Color.FromArgb(64, 64, 64),
            .SelectionForeColor = DosForeColor,
            .Font = CreateDosFont(12.0F)
        }

        ' These help reduce "modern" UI affordances.
        grid.CellBorderStyle = DataGridViewCellBorderStyle.Single
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
    End Sub

    ' --- Safe setters ------------------------------------------------------------

    Private Sub SafeSetBackFore(ctrl As Control, back As Color, fore As Color)
        Try
            ctrl.BackColor = back
        Catch
            ' Some controls may throw; ignore
        End Try

        Try
            ctrl.ForeColor = fore
        Catch
            ' Some controls may throw; ignore
        End Try
    End Sub

    Private Sub SafeSetFont(ctrl As Control, f As Font)
        Try
            ctrl.Font = f
        Catch
            ' Some controls may throw; ignore
        End Try
    End Sub

End Module