Imports SharpPcap
Imports System.IO
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports PacketDotNet
Public Class Library
    Dim device As ICaptureDevice
    Public listener As UdpClient = New UdpClient()
    Public groupEP As IPEndPoint
    Public IP As String
    Public log1 As StreamWriter = New StreamWriter("log.txt", True)
#If DEBUG Then
    Dim remote As String = "192.168.1.2"
#Else
    Dim remote As String = "188.116.56.69"
#End If

    Public updateusers As Boolean = False
    Public users As String()
    Public oldusers() As String = {""}
    Public conn As Boolean = False

    Dim thr As Threading.Thread
    Sub Main(c As String())
        Dim cname As String = c(0)
        Dim nname As String = c(1)
        Dim staticip As Boolean = c(2)

        If staticip Then
            IP = getIP()
            If IP = "31.0.0.10" Then
                IP = "DONOTWANT"
            End If
        Else
            IP = "DONOTWANT"
        End If

        log1.WriteLine("Connecting to DTun Server...")
        groupEP = New IPEndPoint(IPAddress.Parse(remote), 4955)
        listener.Send(Encoding.Default.GetBytes(String.Format("HELO*{0}*{1}*{2}", cname, nname, IP)), Encoding.Default.GetByteCount(String.Format("HELO*{0}*{1}*{2}", cname, nname, IP)), groupEP)

        Dim response() As String = Encoding.Default.GetString(listener.Receive(groupEP)).Split("*")
        IP = response(1)
        users = response(2).Split("^")
        updateusers = True
        Shell("netsh interface ip set address name=DTun4 source=static address=" & response(1) & " mask=255.0.0.0", AppWinStyle.Hide, True, -1)
        'Dim localIPs As IPAddress() = Dns.GetHostAddresses(Dns.GetHostName())
        'For k As Integer = 0 To localIPs.GetUpperBound(0)
        '    If localIPs(k).ToString.StartsWith(response(0)) Then
        '        IP = localIPs(k).ToString
        '    End If
        'Next

        thr = New Threading.Thread(AddressOf ReceivePacket)
        thr.IsBackground = True

        log1.Flush()
        Dim devices As CaptureDeviceList = CaptureDeviceList.Instance()
        Dim chdev As Integer = -1

        'Dim log As String = ""
        log1.WriteLine("Connected with server")
        log1.WriteLine("Scanning for network devices...")
        Dim i As Integer = -1
        For Each dev As ICaptureDevice In devices
            Dim info() As String = dev.ToString.Split(vbLf)
            For j As Integer = 0 To info.GetUpperBound(0)
                If info(j).Contains("FriendlyName: ") Then
                    If info(j).Replace("FriendlyName: ", "") = "DTun4" Then
                        chdev = i + 1
                        Exit For
                    End If
                End If
            Next
            'log = String.Format("{0}" & vbNewLine, dev.ToString)
            i += 1
        Next
        If chdev = -1 Then
            log1.WriteLine("DTun adapter was not found. Try reinstalling it.")
            Exit Sub
        End If
        log1.WriteLine("Device found. Connecting...")


        device = devices(chdev)
        AddHandler device.OnPacketArrival, New SharpPcap.PacketArrivalEventHandler(AddressOf HandlePacket)
        device.Open(DeviceMode.Normal, 30)
        device.StartCapture()
        thr.Start()
        log1.WriteLine("Connected with device.")
        log1.WriteLine("Working...")
        log1.WriteLine()
        conn = True
        log1.Flush()
        'Console.ReadLine()
    End Sub
    Function getIP()
        Dim strHostName As String = System.Net.Dns.GetHostName()
        For i As Integer = 0 To System.Net.Dns.GetHostByName(strHostName).AddressList.Count - 1
            If System.Net.Dns.GetHostByName(strHostName).AddressList(0).ToString().StartsWith("31.") Then
                Return System.Net.Dns.GetHostByName(strHostName).AddressList(0).ToString()
            End If
        Next
    End Function
    Sub SDTun()
        Try
            log1.Flush()
            'log1.Close()
            thr.Abort()
            device.StopCapture()
        Catch
        End Try
    End Sub


    Sub HandlePacket(sender As Object, e As CaptureEventArgs)
        Dim packet As Byte() = e.Packet.Data
        log1.Write("Captured packet: ")
        Dim pack As Packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data)
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)
        Dim icmp As ICMPv4Packet = ICMPv4Packet.GetEncapsulated(pack)

        If (Not ip1 Is Nothing) Then
            ParseTCPIP(pack, packet)
        End If

        If (Not arp Is Nothing) Then
            ParseARP(pack, packet)
        End If

        If (Not icmp Is Nothing) Then
            log1.WriteLine("ICMP NOT IMPLEMENTED!")
        End If


        If (ip1 Is Nothing) And (arp Is Nothing) Then
            log1.WriteLine("nonIP nor arp packet")
            Exit Sub
        End If

        log1.Flush()
    End Sub

    Sub ReceivePacket()
        While True
            Dim packet As Byte() = listener.Receive(groupEP)

            If (Encoding.Default.GetString(packet).StartsWith("KFINE")) Then
                users = Encoding.Default.GetString(packet).Substring(5).Split("^")
                If Not DirectCast(oldusers, IStructuralEquatable).Equals(users, StructuralComparisons.StructuralEqualityComparer) Then
                    oldusers = users
                    updateusers = True
                End If
            End If
            If (Encoding.Default.GetString(packet).StartsWith("RECONNPLS")) Then
                conn = False
                thr.Abort()
                device.StopCapture()
            End If

            Dim pack As Packet = PacketDotNet.Packet.ParsePacket(LinkLayers.Ethernet, packet)
            Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
            Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)


            If (Not ip1 Is Nothing) Then
                'If ip1.DestinationAddress.Equals(IPAddress.Parse(IP)) Then
                device.SendPacket(packet)
                log1.WriteLine("Created IP packet from {0}", ip1.SourceAddress.ToString)
                'Else
                ' log1.WriteLine("Received IP packed intended to another device. Skipped.")
                'End If
            End If

            If (Not arp Is Nothing) Then
                'If arp.TargetProtocolAddress.ToString = IP Then
                device.SendPacket(packet)
                log1.WriteLine("Created ARP packet from {0}", arp.SenderProtocolAddress.ToString)
                'Else
                'log1.WriteLine("Received ARP packed intended to another device. Skipped.")
                'End If
            End If


            log1.Flush()
        End While
    End Sub

    Sub ParseTCPIP(pack As Packet, Packet As Byte())
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        log1.Write("IP packet: ")
        If ip1.Version = IpVersion.IPv4 Then
            'And ip1.SourceAddress.Equals(IPAddress.Parse(IP))
            'If Not ip1.DestinationAddress.Equals(IPAddress.Parse(IP)) And Not ip1.DestinationAddress.Equals(IPAddress.Parse("31.255.255.255")) Then
            If ip1.SourceAddress.Equals(IPAddress.Parse(IP)) Then
                Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
                listener.Send(Packet, Packet.Count(), groupEP)
                log1.WriteLine("Sent to {0}", ip1.DestinationAddress.ToString)
            Else
                log1.WriteLine("Local packet from {0}. Skipped", ip1.SourceAddress.ToString)
            End If
        Else
            log1.WriteLine("v6Packet")
        End If

    End Sub

    Sub ParseARP(pack As Packet, Packet As Byte())
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)
        log1.Write("ARP packet: ")
        'And arp.SenderProtocolAddress.ToString = IP
        'If Not arp.TargetProtocolAddress.ToString = IP Then
        If arp.SenderProtocolAddress.ToString = IP Then
            Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
            listener.Send(Packet, Packet.Count(), groupEP)
            log1.WriteLine("Sent to {0}", arp.TargetProtocolAddress.ToString)
        Else
            log1.WriteLine("Local packet. Skipped")
        End If
    End Sub

    Sub ParseICMP(pack As Packet, Packet As Byte())
        Dim icmp As ICMPv4Packet = ICMPv4Packet.GetEncapsulated(pack)
        log1.Write("ICMP packet: ")
        'And arp.SenderProtocolAddress.ToString = IP
        'If Not arp.TargetProtocolAddress.ToString = IP Then
        If IP Then
            Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
            listener.Send(Packet, Packet.Count(), groupEP)
            'log1.WriteLine("Sent to {0}", icmp.TargetProtocolAddress.ToString)
        Else
            log1.WriteLine("Local packet. Skipped")
        End If
    End Sub
End Class
