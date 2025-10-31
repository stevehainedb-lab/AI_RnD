Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryRequestParameter : Inherits XMLBase

   Private m_Identifier As String
   Private m_Value As String

   Public Sub New()

   End Sub

   Public Sub New(ByVal Identifier As String, ByVal Value As String)
      m_Identifier = Identifier
      m_Value = Value
   End Sub

   Public Property Identifier() As String
      Get
         Return m_Identifier
      End Get
      Set(ByVal value As String)
         m_Identifier = value
      End Set
   End Property

   Public Property Value() As String
      Get
         Return m_Value
      End Get
      Set(ByVal value As String)
         m_Value = value
      End Set
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("Identifier", m_Identifier)
      Writer.WriteAttributeString("Value", m_Value)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)
      m_Identifier = Reader.GetAttribute("Identifier")
      m_Value = Reader.GetAttribute("Value")
      Reader.Read()
   End Sub

   Public Overrides Function Key() As String
      Return Identifier
   End Function
End Class
