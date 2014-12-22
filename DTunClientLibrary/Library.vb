Imports SharpPcap
Imports System.IO
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports PacketDotNet
Imports System.Security.Cryptography
Imports System.Speech.Synthesis

Structure TableEntry
    Dim key As String
    Dim connected As Boolean
    Dim dip As String
    Sub New(key1 As String, dip1 As String)
        key = key1
        connected = False
        dip = dip1
    End Sub
End Structure

Public Class Library
    Dim device As ICaptureDevice
    Public listener As UdpClient = New UdpClient()
    Public direct As UdpClient = New UdpClient()
    Public groupEP As IPEndPoint
    Dim source As New IPEndPoint(IPAddress.Any, 4955)
    Public IP As String
    Dim log1 As StreamWriter '= New StreamWriter("log.txt", True)
#If DEBUG Then
    Dim remote As String = "192.168.1.2"
#Else
    Dim remote As String = "188.116.56.69"
#End If

    Public updateusers As Boolean = False
    Public users As String()
    Public oldusers() As String = {""}
    Public conn As Boolean = False
    Dim serverrsa As New RSACryptoServiceProvider()
    Dim aespass(31) As Byte
    Public state As Integer = 0

    Public Shared icon As System.Windows.Forms.NotifyIcon

    Dim ih As IconHelper

    Public chatlines As New List(Of String)
    Dim chatsender As New UdpClient()

    'Public Shared chatbutton As Windows.Forms.Button
    Public Shared chatbutton

    Private leader As New IPEndPoint(IPAddress.Parse("0.0.0.0"), 1)
    Public connected As Boolean = False
    Public leading As Boolean = False
    Private iptable As New Dictionary(Of IPEndPoint, TableEntry)

    Private log As Boolean

    Dim thr As Threading.Thread

    Public speech As New SpeechSynthesizer()


    Public Sub Main(c As Object())
        If Not log1 Is Nothing Then
            log1.Close()
        End If
        log1 = New StreamWriter("log.txt", True)
        log1.WriteLine("Preparing...")



        Dim cname As String = c(0)
        Dim nname As String = c(1)
        Dim staticip As Boolean = c(2)

        log = c(3)

        icon = c(4)
        ih = New IconHelper

        chatbutton = c(5)

        Dim w As New MyWebClient
        w.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11")
#If Not Debug Then
        remote = Dns.GetHostEntry("apps.disahome.me").AddressList(0).ToString
#End If
        If File.Exists("rsapubkey.txt") And log Then
            serverrsa.FromXmlString(File.ReadAllText("rsapubkey.txt"))
        Else
            remote = Dns.GetHostEntry("apps.disahome.me").AddressList(0).ToString
            Try
                serverrsa.FromXmlString(w.DownloadString("http://dtun4.disahome.me/data/rsapubkey.txt"))
            Catch
                Try
                    serverrsa.FromXmlString(w.DownloadString("http://dtun4.disahome.me/data/rsapubkey.txt"))
                Catch
                    state = 7
                    MsgBox("Can't download server public RSA key.")
                    Exit Sub

                End Try
            End Try
        End If
        w.Dispose()

        log1.WriteLine("Received IP and public key")
        state = 1

        Dim salt1(8) As Byte
        Using rngCsp As New RNGCryptoServiceProvider()
            rngCsp.GetBytes(salt1)
        End Using

        Dim k1 As New Rfc2898DeriveBytes(RandKey(32), salt1, 100000)
        aespass = k1.GetBytes(32)
        k1.Reset()
        k1.Dispose()
        aespass = Encoding.Default.GetBytes(Encoding.Default.GetString(aespass).Replace("|", ",").Replace("^", ".").Replace(":", "?").Replace("*", "l"))
        If log Then
            log1.WriteLine("Generated AES key - DEBUG - " & BitConverter.ToString(aespass).Replace("-", String.Empty))
            log1.WriteLine("AES test: string = test = " & BitConverter.ToString(AES_Encrypt(Encoding.Default.GetBytes("test"))).Replace("-", String.Empty))
            log1.WriteLine("AES test: string = " & BitConverter.ToString(AES_Decrypt(AES_Encrypt(Encoding.Default.GetBytes("test")))).Replace("-", String.Empty))
        Else
            log1.WriteLine("Generated AES key")
        End If

        log1.Flush()

        If staticip Then
            IP = getIP()
            If IP = "0.0.0.0" Then
                staticip = False
                IP = "DONOTWANT"
            End If
            If IP = "32.0.0.10" Then
                IP = "DONOTWANT"
            End If
        Else
            IP = "DONOTWANT"
        End If


        state = 2
        log1.WriteLine("Preparing...")

        groupEP = New IPEndPoint(IPAddress.Parse(remote), 4955)
        source = groupEP


        state = 3
        log1.WriteLine("Connecting to DTun4 Server")

        Dim hellostring As String = String.Format("HELO*{0}*{1}*{2}*{3}", cname, nname, IP, System.Text.Encoding.Default.GetString(serverrsa.Encrypt(aespass, True)))
        listener.Send(Encoding.Default.GetBytes(hellostring), Encoding.Default.GetByteCount(hellostring), groupEP)

        Dim response() As String = Encoding.Default.GetString(listener.Receive(source)).Split({"*"c}, 3)
        IP = response(1)
        users = response(2).Split("^")

        updateusers = True
        Shell("netsh interface ip set address name=DTun4 source=static addr=" & response(1) & " mask=255.0.0.0 gateway=none", AppWinStyle.Hide, True, -1)

        thr = New Threading.Thread(AddressOf ReceivePacket)
        thr.IsBackground = True


        log1.WriteLine("Connected with server")
        log1.WriteLine("Scanning for network devices...")
        state = 4

        log1.Flush()
        Dim devices As CaptureDeviceList = CaptureDeviceList.Instance()
        Dim chdev As Integer = -1

        Dim i As Integer = -1
        For Each dev As ICaptureDevice In devices
            Dim info() As String = dev.ToString.Split(vbLf)
            For j As Integer = 0 To info.GetUpperBound(0)
                If info(j).Contains("FriendlyName: ") Then
                    If info(j).Replace("FriendlyName: ", "").StartsWith("DTun4") Then
                        chdev = i + 1
                        Exit For
                    End If
                End If
            Next
            i += 1
        Next
        If chdev = -1 Then
            log1.WriteLine("DTun adapter was not found. Try reinstalling WinPcap.")
            MsgBox("DTun adapter was not found. Try reinstalling WinPcap.")
            state = 7
            Exit Sub
        End If
        log1.WriteLine("Device found. Connecting...")
        state = 5


        device = devices(chdev)
        AddHandler device.OnPacketArrival, New SharpPcap.PacketArrivalEventHandler(AddressOf HandlePacket)
        device.Open(DeviceMode.Normal, 1)
        device.StartCapture()
        thr.Start()
        log1.WriteLine("Connected with device.")
        log1.WriteLine("Working...")
        log1.WriteLine()
        state = 6
        conn = True
        log1.Flush()
    End Sub
    Public Shared Function getIP()
        Dim strHostName As String = System.Net.Dns.GetHostName()
        For i As Integer = 0 To System.Net.Dns.GetHostByName(strHostName).AddressList.Count - 1
            If System.Net.Dns.GetHostByName(strHostName).AddressList(i).ToString().StartsWith("32.") Then
                Return System.Net.Dns.GetHostByName(strHostName).AddressList(i).ToString()
            End If
        Next
        Return "0.0.0.0"
    End Function
    Public Sub SDTun()
        Try
            log1.Flush()
            thr.Abort()
            device.StopCapture()
        Catch
        End Try
    End Sub


    Private Sub HandlePacket(sender As Object, e As CaptureEventArgs)
        ih.U()
        Dim packet As Byte() = e.Packet.Data
        If log Then
            log1.Write("Captured packet: ")
        End If
        Dim pack As Packet
        Try
            pack = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data)
        Catch
            Exit Sub
        End Try
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)

        If (Not ip1 Is Nothing) Then
            ParseTCPIP(pack, packet)
        ElseIf (Not arp Is Nothing) Then
            ParseARP(pack, packet)
        End If

        If (ip1 Is Nothing) And (arp Is Nothing) Then

            If log Then
                log1.WriteLine("-")
            End If
            Exit Sub
        End If

        If log Then
            log1.Flush()
        End If


    End Sub

    Public Sub SendMessage(mes As String)
        chatsender.Send(System.Text.Encoding.Default.GetBytes("CHAT" & mes), System.Text.Encoding.Default.GetByteCount("CHAT" & mes), New IPEndPoint(IPAddress.Parse("32.255.255.255"), 4956))
        chatlines.Add("You: " & mes)
    End Sub
    Public Sub SendControlMessageReq(ip As String)
        Try
            chatsender.Send(System.Text.Encoding.Default.GetBytes("DTun4CM-REQ"), System.Text.Encoding.Default.GetByteCount("DTun4CM-REQ"), New IPEndPoint(IPAddress.Parse(ip), 4957))
        Catch
        End Try
    End Sub
    Private Sub SendControlMessageReply(ip As String)
        Try
            chatsender.Send(System.Text.Encoding.Default.GetBytes("DTun4CM-REP"), System.Text.Encoding.Default.GetByteCount("DTun4CM-REP"), New IPEndPoint(IPAddress.Parse(ip), 4957))
        Catch
        End Try
    End Sub

    Private Sub ReceivePacket()
        While True
            Try
                source = groupEP
                Dim packet As Byte() = listener.Receive(source)

                Dim message As String = Encoding.Default.GetString(packet)
                If (message.StartsWith("RECONNPLS")) Then
                    conn = False
                    thr.Abort()
                    device.StopCapture()
                    Exit Sub
                End If

                If iptable.ContainsKey(source) Then
                    packet = AES_Decrypt(packet, iptable(source).key)
                Else
                    packet = AES_Decrypt(packet)
                End If

                message = Encoding.Default.GetString(packet)

                If (message.StartsWith("KFINE")) Then
                    Dim conf As String() = message.Split("*")
                    Dim newl As New IPEndPoint(IPAddress.Parse(conf(1).Split(":")(0)), conf(1).Split(":")(1))
                    If newl.Address.ToString <> leader.Address.ToString Or newl.Port <> leader.Port Then
                        If newl.Address.ToString = "1.1.1.1" Then
                            leading = True
                            connected = False
                            iptable.Clear()
                            log1.WriteLine("###You became a leader###")
                        ElseIf newl.Address.ToString <> "0.0.0.0" Then
                            leader = newl
                            leading = False
                            connected = True
                            log1.WriteLine("###New leader: {0}###", leader.ToString)
                            listener.Send(AES_Encrypt(Encoding.Default.GetBytes("heeello")), AES_Encrypt(Encoding.Default.GetBytes("heeello")).Count(), leader)
                            listener.Send(AES_Encrypt(Encoding.Default.GetBytes("heeello")), AES_Encrypt(Encoding.Default.GetBytes("heeello")).Count(), leader)
                        Else
                            connected = False
                            leading = False
                        End If
                        leader = newl
                    End If


                    users = conf(0).Substring(5).Split("^")
                    If Not DirectCast(oldusers, IStructuralEquatable).Equals(users, StructuralComparisons.StructuralEqualityComparer) Then
                        oldusers = users
                        updateusers = True
                    End If


                    If leading Then
                        Dim leaddata As String() = conf(2).Split("^")
                        iptable.Clear()
                        For i As Integer = 0 To leaddata.Count - 1
                            Dim ip As String() = leaddata(i).Split("|")(0).Split(":")
                            Try
                                Dim endp As IPEndPoint = New IPEndPoint(IPAddress.Parse(ip(0)), ip(1))
                                'If Not iptable.ContainsKey(endp) Then
                                iptable(endp) = New TableEntry(leaddata(i).Split("|")(1), leaddata(i).Split("|")(2))
                                'End If
                            Catch
                            End Try
                        Next
                    End If


                    Continue While
                End If




                If packet Is {0} Then
                    Continue While
                End If

                ih.R()

                Dim pack As Packet
                Dim ip1 As IpPacket
                Dim arp As ARPPacket
                Try
                    pack = PacketDotNet.Packet.ParsePacket(LinkLayers.Ethernet, packet)
                    ip1 = IpPacket.GetEncapsulated(pack)
                    arp = ARPPacket.GetEncapsulated(pack)
                Catch
                    Continue While
                End Try


                If (Not ip1 Is Nothing) Then
                    device.SendPacket(packet)
                    If leading Then
                        Dim sent As Boolean = False
                        For i As Integer = 0 To iptable.Count() - 1
                            Try
                                If iptable.ElementAt(i).Value.dip = ip1.DestinationAddress.ToString And iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                    Dim packet1 = AES_Encrypt(packet, iptable.ElementAt(i).Value.key)
                                    listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                    sent = True
                                End If
                            Catch e As Exception
                            End Try
                        Next
                        If Not sent Then
                            For i As Integer = 0 To iptable.Count() - 1
                                Try
                                    If iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                        Dim packet1 = AES_Encrypt(packet, iptable.ElementAt(i).Value.key)
                                        listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                    End If
                                Catch e As Exception
                                End Try
                            Next
                        End If
                    End If

                    If message.Contains("CHAT") Then
                        chatlines.Add(ip1.SourceAddress.ToString & ":" & message.Substring(message.IndexOf("C")).Replace("CHAT", ""))
                        log1.WriteLine("Created chat message from {0}", ip1.SourceAddress.ToString)
                        'My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Hand)
                        If message.Substring(message.IndexOf("C")).Replace("CHAT", "").Length > 3 And speech.Volume > 0 Then
                            speech.SpeakAsync(message.Substring(message.IndexOf("C")).Replace("CHAT", ""))
                        End If
                        chatbutton.Content = "Chat (!)"
                        Continue While
                    End If
                    If message.Contains("DTun4CM-REQ") Then
                        If ip1.DestinationAddress.ToString = IP Then
                            SendControlMessageReply(ip1.SourceAddress.ToString)
                            log1.WriteLine("CM-REQ: Replied")
                        End If
                        Continue While
                    End If

                    If log Then
                        log1.WriteLine("*IP from {0}", ip1.SourceAddress.ToString)
                    End If
                End If

                If (Not arp Is Nothing) Then
                    device.SendPacket(packet)
                    If leading Then
                        Dim sent As Boolean = False
                        For i As Integer = 0 To iptable.Count() - 1
                            Try
                                If iptable.ElementAt(i).Value.dip = arp.TargetProtocolAddress.ToString And iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                    Dim packet1 = AES_Encrypt(packet, iptable.ElementAt(i).Value.key)
                                    listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                    sent = True
                                End If
                            Catch e As Exception
                            End Try
                        Next
                        If Not sent Then
                            For i As Integer = 0 To iptable.Count() - 1
                                Try
                                    If iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                        Dim packet1 = AES_Encrypt(packet, iptable.ElementAt(i).Value.key)
                                        listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                    End If
                                Catch e As Exception
                                End Try
                            Next
                        End If
                    End If

                    '
                    If log Then
                        log1.WriteLine("*ARP from {0}", arp.SenderProtocolAddress.ToString)
                    End If
                End If

                log1.Flush()
            Catch e As Exception
            End Try
        End While
    End Sub

    Private Sub ParseTCPIP(pack As Packet, Packet As Byte())
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        If log Then
            log1.Write("IP: ")
        End If
        If ip1.Version = IpVersion.IPv4 Then
            If ip1.SourceAddress.Equals(IPAddress.Parse(IP)) Then
                If connected Then
                    Try
                        Packet = AES_Encrypt(Packet)
                        listener.Send(Packet, Packet.Count(), leader)
                    Catch
                    End Try
                ElseIf Not leading Then
                    Packet = AES_Encrypt(Packet)
                    Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
                    listener.Send(Packet, Packet.Count(), groupEP)
                Else
                    Dim sent As Boolean = False
                    For i As Integer = 0 To iptable.Count() - 1
                        Try
                            If iptable.ElementAt(i).Value.dip = ip1.DestinationAddress.ToString And iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                Dim packet1 = AES_Encrypt(Packet, iptable.ElementAt(i).Value.key)
                                listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                sent = True
                            End If
                        Catch e As Exception
                        End Try
                    Next
                    If Not sent Then
                        For i As Integer = 0 To iptable.Count() - 1
                            Try
                                If iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                    Dim packet1 = AES_Encrypt(Packet, iptable.ElementAt(i).Value.key)
                                    listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                                End If
                            Catch e As Exception
                            End Try
                        Next
                    End If
                End If
                If log Then
                    log1.WriteLine("Sent to {0}", ip1.DestinationAddress.ToString)
                End If
            Else
                If log Then
                    log1.WriteLine("Local packet from {0}. Skipped", ip1.SourceAddress.ToString)
                End If
            End If
        Else
            If log Then
                log1.WriteLine("-")
            End If
        End If

    End Sub

    Private Sub ParseARP(pack As Packet, Packet As Byte())
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)
        If log Then
            log1.Write("ARP: ")
        End If
        If arp.SenderProtocolAddress.ToString = IP Then
            If connected Then
                Try
                    Packet = AES_Encrypt(Packet)
                    listener.Send(Packet, Packet.Count(), leader)
                Catch
                End Try
            ElseIf Not leading Then
                Packet = AES_Encrypt(Packet)
                Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
                listener.Send(Packet, Packet.Count(), groupEP)
            Else
                Dim sent As Boolean = False
                For i As Integer = 0 To iptable.Count() - 1
                    Try
                        If iptable.ElementAt(i).Value.dip = arp.TargetProtocolAddress.ToString And iptable.ElementAt(i).Key.ToString <> source.ToString Then
                            Dim packet1 = AES_Encrypt(Packet, iptable.ElementAt(i).Value.key)
                            listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                            sent = True
                        End If
                    Catch e As Exception
                    End Try
                Next
                If Not sent Then
                    For i As Integer = 0 To iptable.Count() - 1
                        Try
                            If iptable.ElementAt(i).Key.ToString <> source.ToString Then
                                Dim packet1 = AES_Encrypt(Packet, iptable.ElementAt(i).Value.key)
                                listener.Send(packet1, packet1.Count(), iptable.ElementAt(i).Key)
                            End If
                        Catch e As Exception
                        End Try
                    Next
                End If
            End If
            If log Then
                log1.WriteLine("Sent to {0}", arp.TargetProtocolAddress.ToString)
            End If
        Else
            If log Then
                log1.WriteLine("Local packet. Skipped")
            End If
        End If
    End Sub
    Sub SendCSReq()
        Dim text As Byte() = Encoding.Default.GetBytes("CSPLS")
        Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
        listener.Send(text, text.Count(), groupEP)
    End Sub






    Private Overloads Function AES_Decrypt(ByVal in1 As Byte(), ByVal pass As String) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = Encoding.Default.GetBytes(pass)
            AES.Mode = CipherMode.ECB
            Dim AESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Return AESDecrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Private Overloads Function AES_Encrypt(ByVal in1 As Byte(), ByVal pass As String) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = Encoding.Default.GetBytes(pass)
            AES.Mode = CipherMode.ECB
            Dim AESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Return AESEncrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Private Overloads Function AES_Decrypt(ByVal in1 As Byte()) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = aespass
            AES.Mode = CipherMode.ECB
            Dim AESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Return AESDecrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Private Overloads Function AES_Encrypt(ByVal in1 As Byte()) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = aespass
            AES.Mode = CipherMode.ECB
            Dim AESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Return AESEncrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function


    Private Function Rand(ByVal Min As Integer, ByVal Max As Integer) As Integer
        Static Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function

    Private Function RandKey(RequiredStringLength As Integer) As String
        Dim CharArray() As Char = "(CMWXp),./<123wxyz!@#$%^[]{hijkOPY&*>0ab':Z67cKLsQRSEFGHIJ?;8TUdefg-=_+4vmnoV5ABqrD9tul}\|".ToCharArray
        Dim sb As New System.Text.StringBuilder

        For index As Integer = 1 To RequiredStringLength
            sb.Append(CharArray(Rand(0, CharArray.Length)))
        Next

        Return sb.ToString

    End Function

End Class