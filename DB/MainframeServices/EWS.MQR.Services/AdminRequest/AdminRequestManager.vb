Imports EWS.Diagnostics
Imports EWS.MQR.XML
Imports System.Threading
Imports EWS.Network.TCPIP
Imports EWS.MQR.Comms

Public Class AdminRequestManager

   Public Shared Event SendMessage(ByVal Sender As AdminRequestItem, ByVal args As SendDataEventArgs)

#Region " Member Variables  "

   Private Shared m_RequestQueue As New Queue(Of AdminRequestItem)
   Private Shared m_ResponsesSent As Integer
   Private Shared m_RequestsProcessed As Integer

#End Region

#Region " Contsrutors "
   Private Sub New()

   End Sub
#End Region

#Region " Public Methods "

   Public Shared Sub StartProcesses()
      StartRequestProcessorThreads()
   End Sub

   Public Shared Sub StopProcesses()

      ThreadManager.ShutDownThreadStartingWith("AdminRequestQueueProcessor:")

   End Sub

#End Region

#Region " Public Methods "

   Public Shared Sub AddRequestItem(ByVal connectionId As Integer, ByVal Data As TCPAdminRequest)

      m_RequestQueue.Enqueue(New AdminRequestItem(connectionId, Data))

   End Sub

#End Region

#Region " Private Methods "

   Private Shared Sub StartRequestProcessorThreads()

      Dim Threads As Integer = MQRConfig.Current.MQRServiceConfig.AdminProcessorThreads

      NotificationData.UpdateState(Threads & " Processor Thread(s)")
      NotificationData.SetDelegate("RequestProcessed", New GetNotifyDataValue(AddressOf RequestsProcessed))
      NotificationData.SetDelegate("ResponsesSent", New GetNotifyDataValue(AddressOf ResponsesSent))

      For Count As Integer = 1 To Threads
         ThreadManager.Thread("AdminRequestQueueProcessor:" & Count, New ThreadManager.DoWork(AddressOf QueueProcessor), ThreadPriority.Normal, 100, 100, True, True)
      Next
   End Sub

   Private Shared ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", "AdminRequests")
      End Get
   End Property

   Private Shared Function QueueProcessor() As Boolean

      Dim result As Boolean = False

      Try
         If m_RequestQueue.Count > 0 Then
            Dim Request As AdminRequestItem = m_RequestQueue.Dequeue

            NotificationData.SetValue(Thread.CurrentThread.Name, "Processing")

            AddHandler Request.SendMessage, AddressOf DoSendMessage
            Request.Process()

            m_RequestsProcessed += 1

            NotificationData.SetValue(Thread.CurrentThread.Name, "Running")
            result = True
         End If
      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, "AdminRequestManager", EventLogEntryType.Error)
      End Try

   End Function


   Private Shared Sub DoSendMessage(ByVal Sender As AdminRequestItem, ByVal args As SendDataEventArgs)
      Try
         RemoveHandler Sender.SendMessage, AddressOf DoSendMessage
         RaiseEvent SendMessage(Sender, args)
         m_ResponsesSent += 1
      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, "AdminRequestManager", EventLogEntryType.Error)
      End Try
   End Sub

   Private Shared Function ResponsesSent() As String
      Return m_ResponsesSent.ToString
   End Function

   Private Shared Function RequestsProcessed() As String
      Return m_RequestsProcessed.ToString
   End Function

#End Region

End Class
