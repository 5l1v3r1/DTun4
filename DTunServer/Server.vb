Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Security.Cryptography

Module Server
    Dim listener As UdpClient = New UdpClient(4955)
    Dim dstun As New UdpClient
    Dim groupEP As IPEndPoint
    Dim source As IPEndPoint
    Dim clients As New List(Of IPEndPoint)
    Dim networks As New Dictionary(Of String, List(Of Client))
    Dim leaders As New Dictionary(Of String, IPEndPoint)
    Dim log As New IO.StreamWriter("log.txt")
    Sub Main()
        Dim rsa As New RSACryptoServiceProvider()
        
        rsa.FromXmlString(System.IO.File.ReadAllText("priv.key"))

        groupEP = New IPEndPoint(IPAddress.Any, 4955)
        Dim timer As New System.Timers.Timer
        timer.Interval = 1100
        timer.AutoReset = True
        AddHandler timer.Elapsed, AddressOf Status
        timer.Start()
        timer.Enabled = True

        log.WriteLine("Server started successfully")
        log.Flush()
        While True
            Try
                Try
                    source = groupEP
                    Dim packet As Byte() = listener.Receive(source)
                    If Not clients.Contains(source) Then

                        Dim response As String() = Encoding.Default.GetString(packet).Split({"*"c}, 5)
                        If Not response.GetUpperBound(0) = 4 Then
                            log.WriteLine("Old version client")
                            log.Flush()
                            listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), source)
                            Continue While
                        End If

                        clients.Add(source)

                        Dim newip As String = ""
                        If response(3) = "DONOTWANT" Then
                            newip = String.Format("32.{0}.{1}.{2}", GetRandom(0, 255), GetRandom(0, 255), GetRandom(10, 250))
                        Else
                            newip = response(3)
                        End If

                        If Not networks.ContainsKey(response(2)) Then
                            networks(response(2)) = New List(Of Client)
                            'leaders(response(2)) = New IPEndPoint(IPAddress.Parse("0.0.0.0"), 1)
                            leaders(response(2)) = source
                        End If

                        networks(response(2)).Add(New Client(newip, source, response(1), response(2), rsa.Decrypt(System.Text.Encoding.Default.GetBytes(response(4)), True)))
                        Dim mess As String = "HELO*" & newip & "*"
                        For k As Integer = 0 To networks(response(2)).Count - 1
                            mess &= networks(response(2))(k).Name & ":" & networks(response(2))(k).IP & "^"
                        Next

                        listener.Send(Encoding.Default.GetBytes(mess), Encoding.Default.GetByteCount(mess), source)
                        log.WriteLine("Added client from: " & source.Address.ToString & ":" & source.Port.ToString & " - " & response(1))
                        log.Flush()

                        Continue While
                    End If
                    Dim net As String = FindNetwork(source)

                    If (Encoding.Default.GetString(packet) = "INFOPLS") Then
                        If net = "none" Then
                            listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), source)
                        Else
                            Dim cl As Client = FindClient(source)
                            Dim mess As String = "KFINE"
                            For k As Integer = 0 To networks(net).Count - 1
                                mess &= networks(net)(k).Name & ":" & networks(net)(k).IP & "^"
                                If networks(net)(k).EndP.Equals(source) Then
                                    networks(net)(k).Time = 9
                                End If
                            Next

                            If cl.EndP.Address.ToString = leaders(net).Address.ToString And cl.EndP.Port = leaders(net).Port Then
                                mess &= "*1.1.1.1:2*"
                                For j As Integer = 0 To networks(net).Count - 1
                                    If networks(net).ElementAt(j).EndP.ToString <> leaders(net).ToString Then
                                        mess &= networks(net).ElementAt(j).EndP.ToString & "|" & networks(net).ElementAt(j).Key & "|" & networks(net).ElementAt(j).IP & "^"
                                    End If
                                Next
                            Else
                                mess &= "*" & leaders(net).Address.ToString & ":" & leaders(net).Port & "*"
                            End If
                            listener.Send(cl.Encrypt(Encoding.Default.GetBytes(mess)), cl.Encrypt(Encoding.Default.GetBytes(mess)).Count(), source)
                        End If

                        Console.Write("*")
                        Continue While
                    End If
                    If (Encoding.Default.GetString(packet) = "CSPLS") Then
                        If FindClient(source).EndP.ToString = leaders(FindNetwork(source)).ToString Then
                            leaders(FindNetwork(source)) = New IPEndPoint(IPAddress.Parse("0.0.0.0"), 1)
                        End If
                    End If
                    'Dim cl As Client = FindClient(source)
                    Dim pack As Byte() = FindClient(source).Decrypt(packet)
                    For j As Integer = 0 To networks(net).Count - 1
                        If Not networks(net)(j).EndP.Equals(source) Then
                            Dim cl As Client = networks(net)(j)
                            Try
                                listener.Send(cl.Encrypt(pack), cl.Encrypt(pack).Count(), networks(net)(j).EndP)
                            Catch
                            End Try
                        End If
                    Next
                    Console.Write("#")
                Catch ex As System.Net.Sockets.SocketException
                    log.WriteLine("Ex: " & ex.ToString)
                    log.Flush()
                End Try

            Catch ex As Exception
                log.WriteLine("Ex: " & ex.ToString)
                log.Flush()
            End Try

        End While
    End Sub
    Public Function GetRandom(ByVal Min As Integer, ByVal Max As Integer) As Integer
        Static Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function
    Function FindNetwork(cl As IPEndPoint) As String
        For i As Integer = 0 To networks.Count - 1
            For j As Integer = 0 To networks.ElementAt(i).Value.Count - 1
                If networks.ElementAt(i).Value(j).EndP.Equals(cl) Then
                    Return networks.Keys(i)
                End If
            Next
        Next
        Return "none"
    End Function
    Function FindClient(cl As IPEndPoint) As Client
        For i As Integer = 0 To networks.Count - 1
            For j As Integer = 0 To networks.ElementAt(i).Value.Count - 1
                If networks.ElementAt(i).Value(j).EndP.Equals(cl) Then
                    Return networks.ElementAt(i).Value(j)
                End If
            Next
        Next
        Return Nothing
    End Function
    Sub Status()
restart:
        For i As Integer = 0 To networks.Count - 1
            If leaders(networks.ElementAt(i).Key).Port = 3 Then
                leaders(networks.ElementAt(i).Key) = networks(networks.ElementAt(i).Key).ElementAt(0).EndP
                'Console.WriteLine("No leader- new: {0}", leaders(networks.ElementAt(i).Value.ToString))
            End If
            If networks.ElementAt(i).Value.Count = 0 Then
                networks.Remove(networks.ElementAt(i).Key)
                GoTo restart
            End If
            For j As Integer = 0 To networks.ElementAt(i).Value.Count - 1
                If networks.ElementAt(i).Value.ElementAt(j).Time > 0 Then
                    networks(networks.ElementAt(i).Key).ElementAt(j).Time -= 1
                    Continue For
                End If
                If networks.ElementAt(i).Value.ElementAt(j).Time <= 0 Then
                    Try
                        Console.Write("-" & networks.ElementAt(i).Value.ElementAt(j).Name)
                        listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), networks.ElementAt(i).Value.ElementAt(j).EndP)
                    Catch
                    End Try
                    clients.Remove(networks.ElementAt(i).Value.ElementAt(j).EndP)
                    If leaders(networks.ElementAt(i).Key).ToString = networks.ElementAt(i).Value.ElementAt(j).EndP.ToString Then
                        leaders(networks.ElementAt(i).Key) = New IPEndPoint(IPAddress.Parse("0.0.0.0"), 3)
                    End If
                    networks(networks.ElementAt(i).Key).Remove(networks.ElementAt(i).Value.ElementAt(j))
                    'Console.WriteLine("removed leader")
                    Continue For
                End If
            Next
        Next
    End Sub

    Public Function AES_Decrypt(ByVal in1 As Byte(), ByVal pass As Byte()) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = pass
            AES.Mode = CipherMode.ECB
            Dim AESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Return AESDecrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Public Function AES_Encrypt(ByVal in1 As Byte(), ByVal pass As Byte()) As Byte()
       Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = pass
            AES.Mode = CipherMode.ECB
            Dim AESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Return AESEncrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function

End Module
