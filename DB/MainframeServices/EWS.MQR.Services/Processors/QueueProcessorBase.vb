Imports EWS.Diagnostics
Imports System.Threading

Public MustInherit Class QueueProcessorBase(Of T As QueueItemBase)
   Private m_Identifier As String
   Private m_ProcessorType As String

   Protected Sub New(ByVal ProcessorType As String, ByVal Identifier As String)
      m_Identifier = Identifier
      m_ProcessorType = ProcessorType
   End Sub

   Public Sub StartProcess()
      StartQueueProcessorThread()
   End Sub

   Public Sub StopProcess()
      ThreadManager.ShutdownThread(ToString)
   End Sub

   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", m_ProcessorType)
      End Get
   End Property

   Private Sub StartQueueProcessorThread()
      ThreadManager.Thread(ToString, New ThreadManager.DoWork(AddressOf ProcessThread), ThreadPriority.Normal, 5, 100, True, True)
   End Sub

   Public Overrides Function ToString() As String
      Return String.Format("{0}:{1}", Me.GetType.Name, m_Identifier)
   End Function

   Private Function ProcessThread() As Boolean

      Dim HasRun As Boolean = False

      NotificationData.SetValue(ToString, ProcessState.Running.ToString)
      Dim Item As T = Nothing
      Try
         Item = Dequeue()
         If Not Item Is Nothing Then
            HasRun = True

            NotificationData.SetValue(ToString, ProcessState.Processing.ToString)
            Trace.WriteLine(Thread.CurrentThread.Name & " has picked up an item for processing!", TraceLevel.Verbose)
            Item.Metrics.StartedAction(Me.GetType.Name)
            Try
               ProcessItem(Item)
            Finally
               Item.Metrics.EndedAction(Me.GetType.Name)
            End Try
            NotificationData.SetValue(ToString, ProcessState.Running.ToString)
            Trace.WriteLine(Thread.CurrentThread.Name & " has Processed Item.", TraceLevel.Verbose)

         End If

      Catch ex As Exception
         If Item Is Nothing Then
            EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
         Else
            LogException(Item, ex)
         End If
      End Try

      Return HasRun

   End Function

   Protected Shared Function LogException(ByVal Item As T, ByVal ex As Exception) As String
      Dim Msg As String = "REQUEST:" & Item.InstructionSetData & " EXCEPTION:" & ex.ToString
      EventLogging.Log(Msg, Item.GetType.Name, EventLogEntryType.Error)
      Return Msg
   End Function

   Protected MustOverride Sub ProcessItem(ByVal Item As T)

   Protected MustOverride Function Dequeue() As T

   Public Shared Sub Response(ByVal Item As QueueItemBase, ByVal ex As Exception)
      RequestService.RequestRecieved(Item.QueueItemData.RequestID, ex)
   End Sub

   Public Shared Sub Response(ByVal Item As QueueItemBase, ByVal Result As QueryResult)
      RequestService.RequestRecieved(Item.QueueItemData.RequestID, Result)
   End Sub

End Class
