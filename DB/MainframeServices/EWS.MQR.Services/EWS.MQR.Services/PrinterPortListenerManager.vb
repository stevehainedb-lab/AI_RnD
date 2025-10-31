Imports System.Printing

Imports EWS.MQR.XML
Imports EWS.Diagnostics

Friend Class PrinterPortListenerManager
    Private _initialised As Boolean = False
    Private m_PrinterPortListeners As New Dictionary(Of String, PrinterPortListenerProcess)

    Public ReadOnly Property Initialised As Boolean
        Get
            Return _initialised
        End Get
    End Property

    Public Shared Function PrintQueueJobs(PrintServerName As String) As Integer
        Return MQRPrintQueue(PrintServerName).NumberOfJobs
    End Function


    Public ReadOnly Property Listeners() As Integer
        Get
            Return m_PrinterPortListeners.Count
        End Get
    End Property

    Public Function RunningListeners() As Integer
        Dim result As Integer = 0

        For Each item As PrinterPortListenerProcess In m_PrinterPortListeners.Values
            If item.Listening Then
                result += 1
            End If
        Next

        Return result
    End Function

    Private Shared Function MQRPrintQueue(printer_name As String) As PrintQueue
        'Printer name should be printerserver_portNo in HA environment 
        Dim MQRPrintServer As PrintServer
        If MQRConfig.Current.MQRServiceConfig.PrintQueueServer.ToLower = "localhost" Then
            MQRPrintServer = New PrintServer()
        Else
            MQRPrintServer = New PrintServer("\\" & MQRConfig.Current.MQRServiceConfig.PrintQueueServer)
        End If
        Return New PrintQueue(MQRPrintServer, printer_name, PrintSystemDesiredAccess.AdministratePrinter)
        'Return New PrintQueue(MQRPrintServer, MQRConfig.Current.MQRServiceConfig.PrintQueueName, PrintSystemDesiredAccess.AdministratePrinter)
    End Function

    Public Sub New()

        Dim Ports() As String = MQRConfig.Current.MQRServiceConfig.PrinterListeningPorts.Split(","c)

        NotificationData.UpdateState(Ports.Length & " Processor Thread(s)")

        For Each Port As String In Ports
            Port = Port.Trim
            If Not Port = String.Empty Then
                If Not m_PrinterPortListeners.ContainsKey(Port) Then
                    m_PrinterPortListeners.Add(Port, New PrinterPortListenerProcess(CInt(Port)))
                Else
                    EventLogging.Log("Port " & Port & " has already been configured to listen!", Me.GetType.Name, EventLogEntryType.Error)
                End If
            End If
        Next
    End Sub

    Public Sub StartListening()

        Dim HavePurged As Boolean = False
        
        Dim printerServerName As String = MQRConfig.Current.MQRServiceConfig.PrintQueueName

        Do While MQRPrintQueue(printerServerName).NumberOfJobs > 0
            If Not HavePurged Then
                'purge print queue
                MQRPrintQueue(printerServerName).Purge()
                HavePurged = True
            End If

            System.Threading.Thread.Sleep(1000)
            MQRPrintQueue(printerServerName).Refresh()
        Loop

        For Each Listener As PrinterPortListenerProcess In m_PrinterPortListeners.Values
            AddHandler Listener.OnReceivedData, AddressOf OnReceivedData
            Listener.StartListening()
        Next

        _initialised = True
    End Sub

    Public Sub StopListening()

        _initialised = False

        For Each Listener As PrinterPortListenerProcess In m_PrinterPortListeners.Values
            RemoveHandler Listener.OnReceivedData, AddressOf OnReceivedData
            Try
                Listener.StopListening()
            Catch ex As Exception
                EventLogging.Log("Exception while stopping listener!" & ControlChars.NewLine & ex.Message, Me.GetType.Name, EventLogEntryType.Error)
            End Try            
        Next

        Dim HaveAllStopped As Boolean
        Do While Not HaveAllStopped
            HaveAllStopped = True
            For Each Listener As PrinterPortListenerProcess In m_PrinterPortListeners.Values
                If Listener.Listening Then
                    HaveAllStopped = False
                    Exit For
                End If
                If Not Listener.StoppingOrStopped Then
                    Listener.StopListening()
                End If
            Next
            System.Threading.Thread.Sleep(100)
        Loop

        NotificationManager.RemoveProcess("Processors", "PrinterPortListeners")
    End Sub

    Private Sub OnReceivedData(ByVal sender As PrinterPortListenerProcess, ByVal Args As ReceivedDataEventArgs)
        Try
            Dim Item As New CompleteOutputItem(Args.Data)
            QueueManager.CompleteOutputQueueInstance.EnqueueUpdate(Item)
        Catch ex As Exception
            EventLogging.Log("Exception while processing received data!" & ControlChars.NewLine & ex.Message & ControlChars.NewLine & Args.Data.Substring(0, 80), Me.GetType.Name, EventLogEntryType.Error)
        End Try
    End Sub

    Private ReadOnly Property NotificationData() As NotifyData
        Get
            Return NotificationManager.Instance("Processors", "PrinterPortListeners")
        End Get
    End Property
End Class
