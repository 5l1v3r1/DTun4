Imports System.Runtime.InteropServices

''' <summary>
''' For ProgressBar colors
''' </summary>
Module Modify
    Private Declare Function SendMessage Lib "User32" Alias "SendMessageA" (ByVal hWnd As Long, ByVal wMsg As Long, ByVal wParam As Long, lParam As Long) As Long

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=False)> _
    Private Function SendMessage(hWnd As IntPtr, Msg As UInteger, w As IntPtr, l As IntPtr) As IntPtr
    End Function

    <System.Runtime.CompilerServices.Extension()> _
    Public Sub SetState(pBar As ProgressBar, state As Integer)

        '-- Convert state as integer to type IntPtr
        Dim s As IntPtr
        Dim y As Integer = state
        s = IntPtr.op_Explicit(y)

        '-- Modify bar color
        SendMessage(pBar.Handle, 1040, s, IntPtr.Zero)

    End Sub
End Module