<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMainMenu
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        pnlHeader = New Panel()
        lblDateTime = New Label()
        lblMainMenu = New Label()
        tlpRoot = New TableLayoutPanel()
        flpLeft = New FlowLayoutPanel()
        flpRight = New FlowLayoutPanel()
        tmrClock = New Timer(components)
        pnlHeader.SuspendLayout()
        tlpRoot.SuspendLayout()
        SuspendLayout()
        ' 
        ' pnlHeader
        ' 
        pnlHeader.BackColor = Color.Black
        pnlHeader.Controls.Add(lblDateTime)
        pnlHeader.Controls.Add(lblMainMenu)
        pnlHeader.Dock = DockStyle.Top
        pnlHeader.Location = New Point(0, 0)
        pnlHeader.MaximumSize = New Size(0, 48)
        pnlHeader.MinimumSize = New Size(0, 48)
        pnlHeader.Name = "pnlHeader"
        pnlHeader.Size = New Size(1200, 48)
        pnlHeader.TabIndex = 0
        ' 
        ' lblDateTime
        ' 
        lblDateTime.Dock = DockStyle.Fill
        lblDateTime.Font = New Font("Consolas", 14.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblDateTime.ForeColor = Color.Yellow
        lblDateTime.Location = New Point(365, 0)
        lblDateTime.Name = "lblDateTime"
        lblDateTime.Padding = New Padding(0, 10, 10, 0)
        lblDateTime.Size = New Size(835, 48)
        lblDateTime.TabIndex = 1
        lblDateTime.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' lblMainMenu
        ' 
        lblMainMenu.Dock = DockStyle.Left
        lblMainMenu.Font = New Font("Castellar", 36F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblMainMenu.ForeColor = Color.Yellow
        lblMainMenu.Location = New Point(0, 0)
        lblMainMenu.MaximumSize = New Size(300, 50)
        lblMainMenu.MinimumSize = New Size(365, 50)
        lblMainMenu.Name = "lblMainMenu"
        lblMainMenu.Size = New Size(365, 50)
        lblMainMenu.TabIndex = 0
        lblMainMenu.Text = "MAIN MENU"
        lblMainMenu.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' tlpRoot
        ' 
        tlpRoot.BackColor = Color.Black
        tlpRoot.ColumnCount = 2
        tlpRoot.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        tlpRoot.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        tlpRoot.Controls.Add(flpLeft, 0, 0)
        tlpRoot.Controls.Add(flpRight, 1, 0)
        tlpRoot.Dock = DockStyle.Fill
        tlpRoot.Location = New Point(0, 48)
        tlpRoot.Name = "tlpRoot"
        tlpRoot.RowCount = 1
        tlpRoot.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        tlpRoot.Size = New Size(1200, 752)
        tlpRoot.TabIndex = 1
        ' 
        ' flpLeft
        ' 
        flpLeft.AutoScroll = True
        flpLeft.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(45))
        flpLeft.Dock = DockStyle.Fill
        flpLeft.FlowDirection = FlowDirection.TopDown
        flpLeft.Location = New Point(3, 3)
        flpLeft.Name = "flpLeft"
        flpLeft.Padding = New Padding(10)
        flpLeft.Size = New Size(594, 746)
        flpLeft.TabIndex = 0
        flpLeft.WrapContents = False
        ' 
        ' flpRight
        ' 
        flpRight.AutoScroll = True
        flpRight.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(45))
        flpRight.Dock = DockStyle.Fill
        flpRight.FlowDirection = FlowDirection.TopDown
        flpRight.Location = New Point(603, 3)
        flpRight.Name = "flpRight"
        flpRight.Padding = New Padding(10)
        flpRight.Size = New Size(594, 746)
        flpRight.TabIndex = 1
        flpRight.WrapContents = False
        ' 
        ' tmrClock
        ' 
        tmrClock.Enabled = True
        tmrClock.Interval = 1000
        ' 
        ' FormMainMenu
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1200, 800)
        Controls.Add(tlpRoot)
        Controls.Add(pnlHeader)
        KeyPreview = True
        Name = "FormMainMenu"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Main Menu"
        pnlHeader.ResumeLayout(False)
        tlpRoot.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents pnlHeader As Panel
    Friend WithEvents lblMainMenu As Label
    Friend WithEvents lblDateTime As Label

    Friend WithEvents tlpRoot As TableLayoutPanel
    Friend WithEvents flpLeft As FlowLayoutPanel
    Friend WithEvents flpRight As FlowLayoutPanel

    Friend WithEvents tmrClock As Timer
End Class