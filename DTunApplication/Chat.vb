Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

Public Class Chat
    Dim chatlines1 As New List(Of String)
    'Public Shared overlayshow As Boolean = False
    'Shared overlay As New OverlayLib.Overlay
    'Shared img As Bitmap

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            If Not Form1.lib1.chatlines.Count = chatlines1.Count Then
                For i As Integer = chatlines1.Count To Form1.lib1.chatlines.Count - 1
                    Dim mes As String() = Split(Form1.lib1.chatlines(i), {":"c}, 2)
                    For j As Integer = 0 To Form1.lib1.users.Count - 1
                        If Form1.lib1.users(j).Contains(mes(0)) Then
                            mes(0) = Form1.lib1.users(j).Split(":")(0)
                            Exit For
                        End If
                    Next
                    RichTextBox1.AppendText(vbNewLine & mes(0) & ": " & mes(1))
                Next
                chatlines1 = New List(Of String)(Form1.lib1.chatlines)
                Format()
                ScrollToBottom(RichTextBox1)

            End If
        Catch
        End Try
    End Sub
    Sub Format()
        'For line As Integer = 1 To RichTextBox1.Lines.Count() - 1
        '    If RichTextBox1.Lines(line).StartsWith("You:") Then
        '        RichTextBox1.Select(RichTextBox1.GetFirstCharIndexFromLine(line), RichTextBox1.Lines(line).Length)
        '        RichTextBox1.SelectionColor = Color.Black
        '    Else
        '        RichTextBox1.Select(RichTextBox1.GetFirstCharIndexFromLine(line), RichTextBox1.Lines(line).Length)
        '        Dim color1 As String = "#" & GetMd5Hash(System.Security.Cryptography.MD5.Create(), RichTextBox1.Lines(line).Split(":")(0)).Substring(0, 4) & "00"
        '        RichTextBox1.SelectionColor = System.Drawing.ColorTranslator.FromHtml(color1)
        '    End If
        'Next
        Dim lines() As String = RichTextBox1.Text.Split(vbLf)
        Dim startIndex As Integer = 0
        For i As Integer = 0 To lines.Length - 1
            RichTextBox1.Select(startIndex, lines(i).Length)
            If lines(i).StartsWith("You:") Then
                RichTextBox1.SelectionColor = Color.Black
            Else
                Dim color1 As String = "#" & GetMd5Hash(System.Security.Cryptography.MD5.Create(), lines(i).Split(":")(0)).Substring(0, 4) & "00"
                RichTextBox1.SelectionColor = System.Drawing.ColorTranslator.FromHtml(color1)
            End If
            startIndex += lines(i).Length + vbLf.Length
        Next
    End Sub
    Private Sub Chat_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Timer1.Start()
        TextBox1.Focus()

        
        'img = GetControlImage(RichTextBox1)
        'overlay.Renderer = AddressOf OnRender
        ''overlay.Position = New Point(200, 200)
        '' overlay.Size = New Size(100, 100)
        'overlay.Initialise()
        'Dim hk As New OverlayTools.Helpers.KeyboardHook()
        'Dim event1 As OverlayTools.Helpers.KeyboardHook.KeyboardEvent = AddressOf OnKeyboard
        'hk.InstallHooK()
        'overlay.Position = New Point(500, 500)

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form1.lib1.SendMessage(TextBox1.Text)
        TextBox1.Text = ""
        TextBox1.Focus()
    End Sub

    <DllImport("user32.dll", CharSet:=CharSet.Auto)> _
    Private Shared Function SendMessage(hWnd As IntPtr, wMsg As Integer, wParam As IntPtr, lParam As IntPtr) As Integer
    End Function
    Private Const WM_VSCROLL As Integer = 277
    Private Const SB_PAGEBOTTOM As Integer = 7

    Public Shared Sub ScrollToBottom(MyRichTextBox As RichTextBox)
        SendMessage(MyRichTextBox.Handle, WM_VSCROLL, SB_PAGEBOTTOM, IntPtr.Zero)
    End Sub
    Shared Function GetMd5Hash(ByVal md5Hash As MD5, ByVal input As String) As String
        Dim data As Byte() = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input))
        Dim sBuilder As New StringBuilder()
        Dim i As Integer
        For i = 0 To data.Length - 1
            sBuilder.Append(data(i).ToString("x2"))
        Next i
        Return sBuilder.ToString()

    End Function




    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Button1_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub RichTextBox1_LinkClicked(sender As Object, e As LinkClickedEventArgs) Handles RichTextBox1.LinkClicked
        System.Diagnostics.Process.Start(e.LinkText)
    End Sub








    Private Declare Function FlashWindowEx Lib "User32" (ByRef fwInfo As FLASHWINFO) As Boolean

    Public Const FLASHW_ALL As UInt32 = 3


    Public Structure FLASHWINFO
        Public cbSize As UInt32
        Public hwnd As IntPtr
        Public dwFlags As UInt32
        Public uCount As UInt32
        Public dwTimeout As UInt32
    End Structure




    ' Return a Bitmap holding an image of the control.
    'Public Function GetControlImage(ByVal ctl As Control) As Bitmap
    '    Dim bm As New Bitmap(ctl.Width, ctl.Height)
    '    ctl.DrawToBitmap(bm, New Rectangle(0, 0, ctl.Width, ctl.Height))
    '    Return bm
    'End Function
    'Public Shared Sub OnKeyboard(k As Keys)
    '    If k = Keys.F11 Then
    '        overlayshow = False
    '    End If
    '    If k = Keys.F12 Then
    '        overlayshow = True
    '        Render(Nothing, EventArgs.Empty)
    '    End If
    'End Sub
    'Public Shared Sub OnRender(g As Graphics)
    '    g.Clear(Color.White)


    '    g.ResetTransform()
    '    g.DrawImage(img, New Point(0, 0))

    'End Sub

    'Public Shared Sub Render(sender As Object, args As EventArgs)
    '    If Not overlayshow Then
    '        Return
    '    End If

    '    overlay.Update()
    'End Sub

End Class