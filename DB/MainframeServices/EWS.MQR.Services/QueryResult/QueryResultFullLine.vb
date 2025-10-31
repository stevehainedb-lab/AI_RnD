Imports EWS.XML.Utils
Imports EWS.XML.Utils.Utils

Public Class QueryResultFullLine : Inherits XMLBase

   Private m_Value As String

   Public Property Value() As String
      Get
         Return m_Value
      End Get
      Set(ByVal Value As String)
         m_Value = Value
      End Set
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlTextWriter)
      Writer.WriteCData(m_Value)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)
      m_Value = Reader.ReadElementContentAsString
   End Sub
End Class
