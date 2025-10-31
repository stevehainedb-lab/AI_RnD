Imports EWS.MQR.XML

Public Class CompleteOutputQueue : Inherits SyncQueueBase(Of CompleteOutputItem)
   Public Sub New()
      MyBase.New(MQRConfig.Current.MQRServiceConfig.CompleteOutputTimeoutSeconds)
   End Sub




   'Public Sub MoreData(ByVal Data As String)
   '   Dim NewItem As New CompleteOutputItem(Data)

   '   'Dim ExistingCompleteOutputItem As CompleteOutputItem = MyBase.Dequeue(NewItem.TOPSAltName)

   '   'If Not ExistingCompleteOutputItem Is Nothing Then
   '   '   NewItem.MergeData(ExistingCompleteOutputItem)
   '   'End If

   '   Enqueue(NewItem)

   'End Sub



End Class
