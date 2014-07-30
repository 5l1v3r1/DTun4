Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.IO
Imports System.Drawing.Imaging


Public Class ClientListBox


    Public Sub New(text_ As String, ping_ As String, status As Integer)
        Name = text_
        Ping = ping_

        If status = 0 Then
            _icon = toIcon(My.Resources._0)
            Color = "Red"
        ElseIf status = 1 Then
            _icon = toIcon(My.Resources._1)
            Color = "YellowGreen"
        ElseIf status = 2 Then
            _icon = toIcon(My.Resources._2)
            If ping_ < 100 Then
                Color = "Green"
            ElseIf ping_ < 300 Then
                Color = "YellowGreen"
            Else
                Color = "Red"
            End If
        ElseIf status = 3 Then
            _icon = toIcon(My.Resources._3)
            Color = "Blue"
        End If
    End Sub

    Public Property Name() As String
        Get
            Return _text
        End Get
        Set(value As String)
            _text = value
        End Set
    End Property
    Private _text As String
    Public Property Ping() As String
        Get
            Return _ping
        End Get
        Set(value As String)
            _ping = value
        End Set
    End Property
    Private _ping As String
    Public Property Color() As String
        Get
            Return _color
        End Get
        Set(value As String)
            _color = value
        End Set
    End Property
    Private _color As String
    ReadOnly Property Image() As System.Windows.Media.ImageSource
        Get
            Return _icon
        End Get

    End Property
    Private _icon As System.Windows.Media.ImageSource
    Function toIcon(bitmap As System.Drawing.Icon)
        Using memory As New MemoryStream()
            bitmap.Save(memory)
            memory.Position = 0
            Dim bitmapImage1 As New BitmapImage()
            bitmapImage1.BeginInit()
            bitmapImage1.StreamSource = memory
            bitmapImage1.CacheOption = BitmapCacheOption.OnLoad
            bitmapImage1.EndInit()
            Return bitmapImage1
        End Using
    End Function

    Overrides Function ToString() As String
        Return Name
    End Function
End Class

Public Class ClientCollection
    Inherits List(Of ClientListBox)
End Class
