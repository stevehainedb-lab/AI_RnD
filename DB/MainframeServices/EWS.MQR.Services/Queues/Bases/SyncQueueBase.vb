
Imports EWS.MQR.XML
Imports System.Threading
Imports EWS.Diagnostics

Public MustInherit Class SyncQueueBase(Of T As QueueItemBase)

   'Public MustOverride Function QueueTimeOutPeriod() As TimeSpan

#Region " Member Varibles "

   Public Const NOTIFY_CATEGORY As String = "Queues"

   Private m_ItemsDictionary As New SyncLockDictionaryQueue(Of String, T)

   Private m_ItemsTimedOut As Long = 0
   Private m_ItemsAdded As Long = 0
   Private m_Initalised As Boolean = False
   Private m_Timeoutperiod As TimeSpan = TimeSpan.Zero

   Private m_TimeOutSeconds As Integer = 60

#End Region

#Region " Contructors "
   Public Sub New(QueueTimeOutSeconds As Integer)
      m_TimeOutSeconds = QueueTimeOutSeconds
   End Sub
#End Region

#Region " Public Methods "

   Public Function QueueTimeOutPeriod() As TimeSpan
      If m_Timeoutperiod = TimeSpan.Zero Then
         m_Timeoutperiod = New TimeSpan(0, 0, m_TimeOutSeconds)
      End If
      Return m_Timeoutperiod
   End Function

   Public Sub Enqueue(ByVal Item As T)

      If m_Initalised Then
         Item.Metrics.StartedAction(Me.GetType.Name)
         m_ItemsDictionary.Add(Item.Key, Item)
         m_ItemsAdded += 1
         Trace.WriteLine(ToString() & " Item Enqueued", TraceLevel.Verbose)
      End If

   End Sub

   Public Sub EnqueueUpdate(ByVal Item As T)

      If m_Initalised Then
         Item.Metrics.StartedAction(Me.GetType.Name)
         m_ItemsDictionary.AddUpdate(Item.Key, Item)
         m_ItemsAdded += 1
         Trace.WriteLine(ToString() & " Item Enqueued", TraceLevel.Verbose)
      End If

   End Sub

   Public Function Dequeue() As T
      If m_Initalised Then
         Dim Item As T = m_ItemsDictionary.Dequeue
         If Not Item Is Nothing Then
            Item.Metrics.EndedAction(Me.GetType.Name)
            Trace.WriteLine(ToString() & " Item Dequeued", TraceLevel.Verbose)
         End If

         Return Item
      Else
         Return Nothing
      End If
   End Function

   Public Function Dequeue(ByVal Key As String) As T
      If m_Initalised Then
         Dim Item As T = m_ItemsDictionary.Dequeue(Key)
         If Not Item Is Nothing Then
            Item.Metrics.EndedAction(Me.GetType.Name)
            Trace.WriteLine(ToString() & " Item Dequeued by Key: " & Key, TraceLevel.Verbose)
         End If

         Return Item
      Else
         Return Nothing
      End If
   End Function

   Public Function Count() As Integer
      Return m_ItemsDictionary.Count
   End Function

   Public Overrides Function ToString() As String
      Return Me.GetType.Name & ": " & Count.ToString & " Items"
   End Function

   Public Sub Initalise()
      m_Initalised = True
      NotificationData.SetStateDelegate(New GetNotifyDataValue(AddressOf ToString))
      NotificationData.SetDelegate("ItemsAdded", New GetNotifyDataValue(AddressOf ItemsAdded))
      NotificationData.SetDelegate("TimedOutItems", New GetNotifyDataValue(AddressOf ItemsTimedOut))
   End Sub

   Public Sub Close()
      m_Initalised = False
      CloseDown()
      m_ItemsDictionary.Clear()
      NotificationManager.RemoveProcess(NOTIFY_CATEGORY, ToString)
   End Sub

   Protected Overridable Function DequeueExpiredItem() As T

      If m_Initalised Then
         Dim Result As T = Nothing

         For Each QueueItem As T In m_ItemsDictionary.Values
            If QueueItem.HasTimedOut(QueueTimeOutPeriod) Then
               Result = QueueItem
               m_ItemsDictionary.Remove(QueueItem.Key)
               Exit For
            End If
         Next

         Return Result
      Else
         Return Nothing
      End If

   End Function

   Public Function QueueData() As QueueData
      Dim Result As New QueueData

      Result.Name = Me.GetType.Name
      Result.ItemsAdded = m_ItemsAdded
      Result.ItemsTimedOut = m_ItemsTimedOut

      For Each Item As QueueItemBase In m_ItemsDictionary.Values
         If Result.Items.ContainsKey(Item.Key) Then
            Result.Items(Item.Key) = Item.GetQueueItemData
         Else
            Result.Items.Add(Item.Key, Item.GetQueueItemData)
         End If
      Next

      Return Result
   End Function

   Public Overridable Sub CheckQueueForTimedOutItems()

      Dim TimedOutItem As QueueItemBase = DequeueExpiredItem()

      Do While Not TimedOutItem Is Nothing
         ProcessExpiredItem(TimedOutItem)
         QueueProcessorBase(Of T).Response(TimedOutItem, New TimeoutException("Expired " & Me.GetType.Name & " Item. " & TimedOutItem.QueueItemData.ToString & " Expired At " & TimedOutItem.TimeOutTimeUTC & ". After " & TimedOutItem.TimeOutPeriod(QueueTimeOutPeriod).TotalSeconds & " Seconds."))

         'get the next one
         TimedOutItem = DequeueExpiredItem()
         m_ItemsTimedOut += 1
      Loop

   End Sub

   Protected Overridable Sub CloseDown()

   End Sub

   Public Overridable Sub ProcessExpiredItem(ByVal TimedOutItem As QueueItemBase)

   End Sub

#End Region

#Region " Private Methods "

   Private Function ItemsAdded() As String
      Return m_ItemsAdded.ToString
   End Function

   Private Function ItemsTimedOut() As String
      Return m_ItemsTimedOut.ToString
   End Function

   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance(NOTIFY_CATEGORY, Me.GetType.Name)
      End Get
   End Property

#End Region



End Class
