Imports System.Runtime.InteropServices

Public Class Chat
    Dim chatlines1 As New List(Of String)
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            If Not Form1.lib1.chatlines.Equals(chatlines1) Then
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
                ScrollToBottom(RichTextBox1)
            End If
        Catch
        End Try
    End Sub

    Private Sub Chat_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Timer1.Start()
        TextBox1.Focus()
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


    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Button1_Click(Nothing, Nothing)
        End If
    End Sub
End Class