Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports EWS.Network.TCPIP
Imports EWS.MQR.Services.MQRManager

Public Class ResponseQueueProcessorManager

   Public Delegate Sub ResponseSendMessageDelegate(ByVal args As SendDataEventArgs)

#Region " Member Variables  "

   Private m_Processors As New List(Of ResponseQueueProcessor)
   Const QUEUE_PROCESSOR_TYPE As String = "Response"

#End Region

#Region " Constructors "

   Public Sub New(ByVal SendMessageMethod As ResponseSendMessageDelegate)
      NotificationData.UpdateState(MQRConfig.Current.MQRServiceConfig.ResponseQueueProcessorThreads & " Processor Thread(s)")
      For Count As Integer = 1 To MQRConfig.Current.MQRServiceConfig.ResponseQueueProcessorThreads
         m_Processors.Add(New ResponseQueueProcessor(QUEUE_PROCESSOR_TYPE, Count.ToString, SendMessageMethod))
      Next
   End Sub

#End Region

#Region " Public Methods "

   Public Sub StartProcesses()
      For Each item As ResponseQueueProcessor In m_Processors
         item.StartProcess()
      Next
   End Sub

   Public Sub StopProcesses()
      For Each item As ResponseQueueProcessor In m_Processors
         item.StopProcess()
      Next

      NotificationManager.RemoveProcess("Processors", QUEUE_PROCESSOR_TYPE)

   End Sub

#End Region

#Region " Private Methods "


   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", QUEUE_PROCESSOR_TYPE)
      End Get
   End Property


#End Region

End Class



