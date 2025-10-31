Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports System.Text.RegularExpressions

Public Class CompleteOutputItem : Inherits QueueItemBase

   Private m_ResponseData As String
   Private m_TOPSAltName As String

   Public Sub New(ByVal ResponseData As String)
      MyBase.New(CoreQueueItemData.Empty, New RequestMetrics)
      m_TOPSAltName = ExtractTOPSAltName(ResponseData)
      QueueItemData.RecievedResponse(ExtractResponseDate(ResponseData))
      m_ResponseData = ResponseData
      FileWriter.Write("PrinterPortListener", ResponseData, m_TOPSAltName)
   End Sub

   Public ReadOnly Property ResponseData() As String
      Get
         Return m_ResponseData
      End Get
   End Property

   Public ReadOnly Property TOPSAltName() As String
      Get
         Return m_TOPSAltName
      End Get
   End Property

   Private Function ExtractTOPSAltName(ByVal Data As String) As String
      Return System.Text.RegularExpressions.Regex.Match(Data, "\A\w{2}[0-9]{5}").Value
   End Function

   Public Overrides ReadOnly Property Key() As String
      Get
         Return m_TOPSAltName
      End Get
   End Property

   'Public Sub MergeData(ByVal ExistingItem As CompleteOutputItem)

   '   m_CollectedData = ExistingItem.CollectedData & m_CollectedData

   'End Sub
End Class
