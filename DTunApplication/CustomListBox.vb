Namespace Toolset.Controls
    Public Class CustomDrawListBox
        Inherits ListBox
        Dim _1 As Icon = My.Resources._1
        Dim _0 As Icon = My.Resources._0
        Dim _2 As Icon = My.Resources._2
        Dim _3 As Icon = My.Resources._3
        Public Sub New()
            Me.DrawMode = Windows.Forms.DrawMode.OwnerDrawFixed

            Me.ItemHeight = 16
        End Sub

        Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
            e.DrawBackground()
            If e.Index >= Me.Items.Count OrElse e.Index <= -1 Then
                Return
            End If

            Dim item As Object = Me.Items(e.Index)
            If item Is Nothing Then
                Return
            End If



            Dim text As String = item.ToString()
            Dim stringSize As SizeF = e.Graphics.MeasureString(text, Me.Font)
            If DTun4.Form1.status.ContainsKey(text) Then
                If DTun4.Form1.status(text).status = 0 Then
                    e.Graphics.DrawIcon(_0, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Red), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    e.Graphics.DrawString("999", Me.Font, Brushes.Red, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                ElseIf DTun4.Form1.status(text).status = 1 Then
                    e.Graphics.DrawIcon(_1, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.YellowGreen), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    e.Graphics.DrawString("N/A", Me.Font, Brushes.YellowGreen, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                ElseIf DTun4.Form1.status(text).status = 2 Then
                    e.Graphics.DrawIcon(_2, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Green), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    If DTun4.Form1.status(text).pingtime < 100 Then
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.Green, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    ElseIf DTun4.Form1.status(text).pingtime < 300 Then
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.YellowGreen, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    Else
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.Red, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    End If

                End If
            Else
                e.Graphics.DrawIcon(_3, 0, e.Bounds.Y)
                e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Blue), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
            End If
        End Sub
    End Class
End Namespace