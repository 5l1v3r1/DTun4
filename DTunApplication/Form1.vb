Imports System.Text
Imports System.Net

Public Class Form1
    Dim lib1 As Library
    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        
        Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll")
        Label7.Text = fvi.FileMajorPart & "." & fvi.FileMinorPart
        TextBox1.Text = My.Settings.tb1
        TextBox2.Text = My.Settings.tb2
        CheckBox1.Checked = My.Settings.ch1

        If TextBox2.Text = "" Then
            TextBox2.Text = Environment.MachineName
        End If
#If Not Debug Then
        If Not (Command$().ToLower.Contains("-updated")) Then
            MessageBox.Show("Start application using updater")
            Environment.Exit(1)
            Exit Sub
        End If
#End If
        WindowState = FormWindowState.Normal
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        NotifyIcon1.Visible = False
        NotifyIcon1.Dispose()
        My.Settings.tb1 = TextBox1.Text
        My.Settings.tb2 = TextBox2.Text
        My.Settings.ch1 = CheckBox1.Checked
        My.Settings.Save()
        If Not lib1 Is Nothing Then
            lib1.SDTun()
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TextBox1.Text.Contains("*") Or TextBox2.Text.Contains("*") Or TextBox1.Text.Contains("^") Or TextBox2.Text.Contains("^") Or TextBox1.Text.Contains(":") Or TextBox2.Text.Contains(":") Then
            MsgBox("Names can contain neither *, ^ nor :")
            Exit Sub
        End If
        lib1 = New Library
        Dim thr As New Threading.Thread(AddressOf lib1.Main)
        thr.IsBackground = True
        Dim c(2) As String
        c(0) = TextBox2.Text
        c(1) = TextBox1.Text
        c(2) = CheckBox1.Checked
        thr.Start(c)
        'lib1.Main(TextBox2.Text, TextBox1.Text)

        TextBox1.Enabled = False
        TextBox2.Enabled = False
        Button1.Enabled = False
        Label5.Text = "Communicating..."
        Timer1.Start()

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If lib1.updateusers Then
            Label5.Text = "Establishing connection..."
            lib1.updateusers = False
            For i As Integer = 0 To lib1.users.Count - 2
                ListBox1.Items.Add(lib1.users(i))
            Next
            Label9.Text = lib1.IP
        End If

        If lib1.conn Then

            Label5.Text = "Connected."
            Timer2.Enabled = True
            Timer1.Enabled = False
        End If
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If Not lib1.conn Then
            Label5.Text = "Connection lost. Reconnecting..."
            lib1.SDTun()
            Button1_Click(Nothing, Nothing)
            Timer2.Enabled = False
        End If

        If lib1.updateusers Then
            lib1.updateusers = False
            ListBox1.Items.Clear()
            For i As Integer = 0 To lib1.users.Count - 2
                ListBox1.Items.Add(lib1.users(i))
            Next
        Else
            Dim source As IPEndPoint = New IPEndPoint(IPAddress.Any, 4955)
            lib1.listener.Send(Encoding.Default.GetBytes("INFOPLS"), Encoding.Default.GetByteCount("INFOPLS"), lib1.groupEP)
        End If
    End Sub

    Private Sub ListBox1_Click(sender As Object, e As EventArgs) Handles ListBox1.Click
        If Not ListBox1.SelectedItem Is Nothing Then
            ToolTip1.SetToolTip(Me.ListBox1, "Click to copy IP to clipboard")
            Clipboard.SetText(ListBox1.SelectedItem.ToString.Split(":")(1))
            ListBox1.ClearSelected()
        End If
    End Sub

    Private Sub NotifyIcon1_Click(sender As Object, e As EventArgs) Handles NotifyIcon1.Click
        Show()
        WindowState = FormWindowState.Normal
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If FormWindowState.Minimized = WindowState Then
            Hide()
        End If
    End Sub

End Class
