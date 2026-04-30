<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAbout
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
        Me.lblAbout = New Label()
        Me.lblAppName = New Label()
        Me.lblDevelopedBy = New Label()
        Me.picDeveloper = New PictureBox()
        Me.rtbInfo = New RichTextBox()
        Me.pnlBottom = New Panel()
        Me.btnOK = New Button()

        CType(Me.picDeveloper, ComponentModel.ISupportInitialize).BeginInit()
        Me.pnlBottom.SuspendLayout()
        Me.SuspendLayout()

        ' -------------------------
        ' lblAbout
        ' -------------------------
        Me.lblAbout.Dock = DockStyle.Top
        Me.lblAbout.Font = New Font("Segoe UI", 8.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Me.lblAbout.Location = New Point(0, 0)
        Me.lblAbout.Name = "lblAbout"
        Me.lblAbout.Size = New Size(800, 22)
        Me.lblAbout.TabIndex = 0
        Me.lblAbout.Text = "About:"
        Me.lblAbout.TextAlign = ContentAlignment.MiddleCenter

        ' -------------------------
        ' lblAppName
        ' -------------------------
        Me.lblAppName.Dock = DockStyle.Top
        Me.lblAppName.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Me.lblAppName.ForeColor = Color.DarkRed
        Me.lblAppName.Location = New Point(0, 0)
        Me.lblAppName.Name = "lblAppName"
        Me.lblAppName.Size = New Size(800, 30)
        Me.lblAppName.TabIndex = 1
        Me.lblAppName.Text = "Active Magnetic Inspection Main Menu Application"
        Me.lblAppName.TextAlign = ContentAlignment.MiddleCenter

        ' -------------------------
        ' lblDevelopedBy
        ' -------------------------
        Me.lblDevelopedBy.Dock = DockStyle.Top
        Me.lblDevelopedBy.Font = New Font("Segoe UI", 8.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Me.lblDevelopedBy.ForeColor = SystemColors.ControlText
        Me.lblDevelopedBy.BackColor = SystemColors.Control
        Me.lblDevelopedBy.Location = New Point(0, 0)
        Me.lblDevelopedBy.Name = "lblDevelopedBy"
        Me.lblDevelopedBy.Size = New Size(800, 22)
        Me.lblDevelopedBy.TabIndex = 2
        Me.lblDevelopedBy.Text = "Developed By:"
        Me.lblDevelopedBy.TextAlign = ContentAlignment.MiddleCenter

        ' -------------------------
        ' picDeveloper
        ' -------------------------
        Me.picDeveloper.Dock = DockStyle.Top
        Me.picDeveloper.Name = "picDeveloper"
        Me.picDeveloper.Size = New Size(800, 60)
        Me.picDeveloper.SizeMode = PictureBoxSizeMode.Zoom
        Me.picDeveloper.TabIndex = 3
        Me.picDeveloper.TabStop = False

        ' -------------------------
        ' pnlBottom
        ' -------------------------
        Me.pnlBottom.Dock = DockStyle.Bottom
        Me.pnlBottom.Name = "pnlBottom"
        Me.pnlBottom.Size = New Size(800, 45)
        Me.pnlBottom.TabIndex = 6

        ' btnOK
        Me.btnOK.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        Me.btnOK.DialogResult = DialogResult.OK
        Me.btnOK.Location = New Point(673, 11)
        Me.btnOK.Name = "btnOK"
        Me.btnOK.Size = New Size(75, 23)
        Me.btnOK.TabIndex = 0
        Me.btnOK.Text = "OK"
        Me.btnOK.UseVisualStyleBackColor = True

        Me.pnlBottom.Controls.Add(Me.btnOK)

        ' -------------------------
        ' rtbInfo
        ' -------------------------
        Me.rtbInfo.BorderStyle = BorderStyle.FixedSingle
        Me.rtbInfo.Dock = DockStyle.Fill
        Me.rtbInfo.Font = New Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Me.rtbInfo.Name = "rtbInfo"
        Me.rtbInfo.ReadOnly = True
        Me.rtbInfo.TabIndex = 5
        Me.rtbInfo.Text = ""

        ' -------------------------
        ' FormAbout
        ' -------------------------
        Me.AcceptButton = Me.btnOK
        Me.AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(760, 550)

        Me.Controls.Add(Me.rtbInfo)
        Me.Controls.Add(Me.pnlBottom)
        Me.Controls.Add(Me.picDeveloper)
        Me.Controls.Add(Me.lblDevelopedBy)
        Me.Controls.Add(Me.lblAppName)
        Me.Controls.Add(Me.lblAbout)

        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormAbout"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Text = "About"

        CType(Me.picDeveloper, ComponentModel.ISupportInitialize).EndInit()
        Me.pnlBottom.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents lblAbout As Label
    Friend WithEvents lblAppName As Label
    Friend WithEvents lblDevelopedBy As Label
    Friend WithEvents picDeveloper As PictureBox
    Friend WithEvents rtbInfo As RichTextBox
    Friend WithEvents pnlBottom As Panel
    Friend WithEvents btnOK As Button
End Class