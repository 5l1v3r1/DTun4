Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Security.Cryptography

Class Client
    Private ipadd As String
    Private ep As IPEndPoint
    Private nname As String
    Private net As String
    Private timeout As Integer
    Private aes As String
    Property IP() As String
        Get
            Return ipadd
        End Get
        Set(value As String)
            ipadd = value
        End Set
    End Property
    Property EndP() As IPEndPoint
        Get
            Return ep
        End Get
        Set(value As IPEndPoint)
            ep = value
        End Set
    End Property
    Property Name() As String
        Get
            Return nname
        End Get
        Set(value As String)
            nname = value
        End Set
    End Property
    Property Network() As String
        Get
            Return net
        End Get
        Set(value As String)
            net = value
        End Set
    End Property
    Property Time() As Integer
        Get
            Return timeout
        End Get
        Set(value As Integer)
            timeout = value
        End Set
    End Property
    Public Function Encrypt(str As Byte()) As Byte()
        Return AES_Encrypt(str, aes)
    End Function
    Public Function Decrypt(str As Byte()) As Byte()
        Return AES_Decrypt(str, aes)
    End Function

    Sub New(a1 As String, a2 As IPEndPoint, a3 As String, a4 As String, AESKEY As String)
        ipadd = a1
        ep = a2
        nname = a3
        net = a4
        timeout = 5
        aes = AESKEY
    End Sub
End Class
Module Server
    Dim listener As UdpClient = New UdpClient(4955)
    Dim groupEP As IPEndPoint
    Dim source As IPEndPoint
    Dim clients As New List(Of IPEndPoint)
    Dim networks As New Dictionary(Of String, List(Of Client))

    Sub Main()
        Dim rsa As New RSACryptoServiceProvider()
        'Dim publickey As String = rsa.ToXmlString(True)
        'Console.WriteLine(publickey) 'DEBUG
        'publickey = rsa.ToXmlString(False)
        'Console.WriteLine(publickey) 'DEBUG
        rsa.FromXmlString(System.IO.File.ReadAllText("priv.key"))

        groupEP = New IPEndPoint(IPAddress.Any, 4955)
        Dim timer As New System.Timers.Timer
        timer.Interval = 5000
        timer.AutoReset = True
        AddHandler timer.Elapsed, AddressOf Status
        timer.Start()
        timer.Enabled = True


        While True
            Try
                Try
                    source = groupEP
                    Dim packet As Byte() = listener.Receive(source)
                    If Not clients.Contains(source) Then

                        Dim response As String() = Encoding.Default.GetString(packet).Split({"*"c}, 5)
                        If Not response.GetUpperBound(0) = 4 Then
                            listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), source)
                            Continue While
                        End If

                        clients.Add(source)

                        If Not networks.ContainsKey(response(2)) Then
                            networks(response(2)) = New List(Of Client)
                        End If
                        Dim newip As String = ""
                        If response(3) = "DONOTWANT" Then
                            newip = String.Format("31.{0}.{1}.{2}", GetRandom(0, 255), GetRandom(0, 255), GetRandom(10, 250))
                        Else
                            newip = response(3)
                        End If

                        networks(response(2)).Add(New Client(newip, source, response(1), response(2), System.Text.Encoding.Default.GetString(rsa.Decrypt(System.Text.Encoding.Default.GetBytes(response(4)), True))))
                        Dim mess As String = "HELO*" & newip & "*"
                        For k As Integer = 0 To networks(response(2)).Count - 1
                            mess &= networks(response(2))(k).Name & ":" & networks(response(2))(k).IP & "^"
                        Next
                        ' mess &= "*" & rsa.ToXmlString(False)
                        listener.Send(Encoding.Default.GetBytes(mess), Encoding.Default.GetByteCount(mess), source)

                        Console.WriteLine("Added client from: " & source.Address.ToString & ":" & source.Port.ToString)
                        Continue While
                    End If
                    Dim net As String = FindNetwork(source)

                    If (Encoding.Default.GetString(packet) = "INFOPLS") Then
                        If net = "none" Then
                            Console.WriteLine("@")
                            listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), source)
                        Else
                            Dim mess As String = "KFINE"
                            For k As Integer = 0 To networks(net).Count - 1
                                mess &= networks(net)(k).Name & ":" & networks(net)(k).IP & "^"
                                If networks(net)(k).EndP.Equals(source) Then
                                    networks(net)(k).Time = 5
                                End If
                            Next
                            listener.Send(Encoding.Default.GetBytes(mess), Encoding.Default.GetByteCount(mess), source)
                        End If

                        Console.Write("*")
                        Continue While
                    End If
                    'Dim cl As Client = FindClient(source)
                    Dim pack As Byte() = FindClient(source).Decrypt(packet)
                    For j As Integer = 0 To networks(net).Count - 1
                        If Not networks(net)(j).EndP.Equals(source) Then
                            Dim cl As Client = networks(net)(j)
                            Try
                                listener.Send(cl.Encrypt(pack), cl.Encrypt(pack).Count(), networks(net)(j).EndP)
                                'listener.Send(packet, packet.Count(), networks(net)(j).EndP)
                            Catch ex As System.Net.Sockets.SocketException
                                clients.Remove(networks(net)(j).EndP)
                                networks(net).Remove(networks(net)(j))
                                Console.WriteLine("Removed disconnected client")
                            End Try
                        End If
                    Next

                    Console.Write("#")
                Catch ex As System.Net.Sockets.SocketException
                    Console.WriteLine("Weird socketexception lol")
                End Try

            Catch ex As Exception
                Console.WriteLine("Ex: " & ex.ToString)
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

        For i As Integer = 0 To networks.Count - 1
            For j As Integer = 0 To networks.ElementAt(i).Value.Count - 1
                If networks.ElementAt(i).Value.ElementAt(j).Time > 0 Then
                    networks(networks.ElementAt(i).Key).ElementAt(j).Time -= 1
                    Continue For
                End If
                If networks.ElementAt(i).Value.ElementAt(j).Time = 0 Then
                    Try
                        Console.Write("-" & networks.ElementAt(i).Value.ElementAt(j).Name)
                        listener.Send(Encoding.Default.GetBytes("RECONNPLS"), Encoding.Default.GetByteCount("RECONNPLS"), networks.ElementAt(i).Value.ElementAt(j).EndP)
                        clients.Remove(networks.ElementAt(i).Value.ElementAt(j).EndP)
                        networks(networks.ElementAt(i).Key).Remove(networks.ElementAt(i).Value.ElementAt(j))
                    Catch
                    End Try

                    Continue For
                End If
            Next
        Next
    End Sub

    Public Function AES_Decrypt(ByVal in1 As Byte(), ByVal pass As String) As Byte()
        Dim input As String = Convert.ToBase64String(in1)

        Dim AES As New System.Security.Cryptography.RijndaelManaged
        Dim Hash_AES As New System.Security.Cryptography.MD5CryptoServiceProvider
        Dim decrypted As String = ""
        Try
            Dim hash(31) As Byte
            Dim temp As Byte() = Hash_AES.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(pass))
            Array.Copy(temp, 0, hash, 0, 16)
            Array.Copy(temp, 0, hash, 15, 16)
            AES.Key = hash
            AES.Mode = CipherMode.ECB
            Dim DESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Dim Buffer As Byte() = Convert.FromBase64String(input)
            decrypted = System.Text.ASCIIEncoding.ASCII.GetString(DESDecrypter.TransformFinalBlock(Buffer, 0, Buffer.Length))
            Return Convert.FromBase64String(decrypted)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Public Function AES_Encrypt(ByVal in1 As Byte(), ByVal pass As String) As Byte()
        Dim input As String = Convert.ToBase64String(in1)


        Dim AES As New System.Security.Cryptography.RijndaelManaged
        Dim Hash_AES As New System.Security.Cryptography.MD5CryptoServiceProvider
        Dim encrypted As String = ""
        Try
            Dim hash(31) As Byte
            Dim temp As Byte() = Hash_AES.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(pass))
            Array.Copy(temp, 0, hash, 0, 16)
            Array.Copy(temp, 0, hash, 15, 16)
            AES.Key = hash
            AES.Mode = CipherMode.ECB
            Dim DESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Dim Buffer As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(input)
            encrypted = Convert.ToBase64String(DESEncrypter.TransformFinalBlock(Buffer, 0, Buffer.Length))
            Return Convert.FromBase64String(encrypted)
        Catch ex As Exception
            Return {0}
        End Try
    End Function

End Module
