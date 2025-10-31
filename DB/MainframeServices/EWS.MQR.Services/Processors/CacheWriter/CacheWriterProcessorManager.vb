Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports System.Data.SqlClient

Public Class CacheWriterProcessorManager

#Region " Member Variables  "

   Private m_Processors As New List(Of CacheWriterProcessor)
   Const QUEUE_PROCESSOR_TYPE As String = "CacheWriter"

#End Region

#Region " Constructors "

   Public Sub New()
      NotificationData.UpdateState(MQRConfig.Current.MQRServiceConfig.CacheWriterThreads & " Processor Thread(s)")
      For Count As Integer = 1 To MQRConfig.Current.MQRServiceConfig.CacheWriterThreads
         m_Processors.Add(New CacheWriterProcessor(QUEUE_PROCESSOR_TYPE, Count.ToString))
      Next
   End Sub

#End Region

#Region " Private Methods "

   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", QUEUE_PROCESSOR_TYPE)
      End Get
   End Property

#End Region

#Region " Public Methods "

   Public Shared Function HaveCacheConnectivity() As Boolean

      Dim Result As Boolean
      Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.CacheConnectionString)

      Try
         Connection.Open()
         Result = True
      Catch ex As Exception
         EventLogging.Log("Error Connecting to " & MQRConfig.Current.MQRServiceConfig.CacheConnectionString & " : " & ex.ToString, "Cache", EventLogEntryType.Error)
         Result = False
      Finally
         With Connection
            .Close()
            .Dispose()
         End With
      End Try

      Return Result

   End Function


   Public Sub StartProcesses()
      For Each item As CacheWriterProcessor In m_Processors
         item.StartProcess()
      Next
   End Sub

   Public Sub StopProcesses()
      For Each item As CacheWriterProcessor In m_Processors
         item.StopProcess()
      Next

      NotificationManager.RemoveProcess("Processors", QUEUE_PROCESSOR_TYPE)
   End Sub

#End Region

End Class


