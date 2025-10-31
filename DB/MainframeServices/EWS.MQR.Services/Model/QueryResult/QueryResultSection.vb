Imports EWS.XML

<Serializable()> _
Public Class QueryResultSection : Inherits XMLBase

   Private m_Identifier As String
   Private m_Rows As New QueryResultRows

   Public Sub New()
      MyBase.New()
   End Sub

   Public Property Identifier() As String
      Get
         Return m_Identifier
      End Get
      Set(ByVal value As String)
         m_Identifier = value
      End Set
   End Property

   Public Sub AddRow(ByVal Row As QueryResultRow)
      Rows.Add(Row.Key, Row)
   End Sub

   Public ReadOnly Property Rows() As QueryResultRows
      Get
         Return m_Rows
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("Identifier", m_Identifier)
      Rows.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)
      m_Identifier = Reader.GetAttribute("Identifier")
      Reader.Read()
      Rows.PopulateFromXMLReader(Reader)

   End Sub

   Public Overrides Function Key() As String
      Return Identifier
   End Function
End Class
