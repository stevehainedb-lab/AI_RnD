Imports EWS.Network.TCPIP
Imports EWS.Diagnostics
Imports System.Threading
Imports EWS.MQR.Comms
Imports EWS.MQR.Services.MQRManager
Imports EWS.MQR.XML

Public Class RequestProcessor

   Private Class QueueItem
      Public ConnectionID As Integer
      Public Message As TCPMessage

      Public Sub New(ByVal TheConnectionID As Integer, ByVal TheMessage As TCPMessage)
         ConnectionID = TheConnectionID
         Message = TheMessage
      End Sub

   End Class

#Region " Member Variables "

   Private Shared m_ValidRequests As Long
   Private Shared m_InvalidRequests As Long

   Private Shared m_Queue As Queue = Queue.Synchronized(New Queue)

#End Region

#Region " Constructors "

   Shared Sub New()
      NotificationData.SetDelegate("ValidRequests", New GetNotifyDataValue(AddressOf ValidRequests))
      NotificationData.SetDelegate("InvalidRequests", New GetNotifyDataValue(AddressOf InvalidRequests))
   End Sub

   Private Sub New()

   End Sub

#End Region

#Region " Public Methods "

   Public Shared Sub StartProcess()
      StartProcessorThread()
   End Sub

   Public Shared Sub StopProcess()
      ThreadManager.ShutdownThread(ThreadName)
   End Sub

   Public Shared Sub MessageReceived(ByVal ConnectionID As Integer, ByVal Data As TCPMessage)
      m_Queue.Enqueue(New QueueItem(ConnectionID, Data))
   End Sub

#End Region

#Region " Private Methods "

   Private Shared ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", "RequestProcessor")
      End Get
   End Property

   Private Shared ReadOnly Property ThreadName() As String
      Get
         Return "QueryRequestProcesor"
      End Get
   End Property

   Private Shared Sub StartProcessorThread()
      ThreadManager.Thread(ThreadName, New ThreadManager.DoWork(AddressOf ProcessThread), ThreadPriority.Normal, 10, 100, True, True)
   End Sub

   Private Shared Function ValidRequests() As String
      Return m_ValidRequests.ToString()
   End Function

   Private Shared Function InvalidRequests() As String
      Return m_InvalidRequests.ToString
   End Function

   Private Shared Function ProcessThread() As Boolean

      Dim HaveRun As Boolean = False

      Try
         If m_Queue.Count > 0 Then
            NotificationData.UpdateState(ProcessState.Processing)
            Do While m_Queue.Count > 0
               ProcessQueueItem(DirectCast(m_Queue.Dequeue, QueueItem))
            Loop
            NotificationData.UpdateState(ProcessState.Running)
            HaveRun = True
         End If

      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, "RequestProcessor", EventLogEntryType.Error)
      End Try

      Return HaveRun

   End Function

   Private Shared Sub ProcessQueueItem(ByVal Item As QueueItem)

      If Item.Message Is Nothing Then
         Dim Msg As String = "Invalid Message Received: no TCP Message."
         QueueProcessorBase(Of ResponseItem).Response(New MQRRetrieveException(Msg), Item.ConnectionID)
         m_InvalidRequests += 1

         'Dim ResponseMessage As New TCPErrorMessage(Msg)
         'QueueManager.ResponseQueueInstance.Enqueue(New ResponseItem(New CoreQueueItemData(Item.ConnectionID), ResponseMessage, New RequestMetrics))
      Else

         If Item.Message.MessageName = TCPMQRRequestMessage.REQUEST_MESSAGE_NAME Then
            Dim RequestMessage As New TCPMQRRequestMessage(Item.Message)
            QueueManager.RequestQueueInstance.Enqueue(New RequestItem(Item.ConnectionID, RequestMessage))

            m_ValidRequests += 1
         Else
            Dim Msg As String = "Invalid Message Received: " & Item.Message.MessageName
            QueueProcessorBase(Of ResponseItem).Response(New MQRRetrieveException(Msg), Item.ConnectionID)
            m_InvalidRequests += 1

            'Dim ResponseMessage As New TCPErrorMessage("Invalid Message Received: " & Item.Message.MessageName)
            'QueueManager.ResponseQueueInstance.Enqueue(New ResponseItem(New CoreQueueItemData(Item.ConnectionID), ResponseMessage, New RequestMetrics))
         End If
      End If
   End Sub

#End Region

End Class
