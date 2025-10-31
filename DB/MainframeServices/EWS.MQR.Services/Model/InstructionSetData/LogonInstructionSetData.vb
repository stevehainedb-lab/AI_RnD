Imports EWS.XML

Public Class LogonInstructionSetData : Inherits XMLBase

   Private m_Name As String
   Private m_CompatableQueries As New QueryInstructionSetDatas

   Public Overrides Function Key() As String
      Return m_Name
   End Function

   Public Property Name() As String
      Get
         Return m_Name
      End Get
      Set(ByVal value As String)
         m_Name = value
      End Set
   End Property

   Public Property CompatableQueries() As QueryInstructionSetDatas
      Get
         Return m_CompatableQueries
      End Get
      Set(ByVal value As QueryInstructionSetDatas)
         m_CompatableQueries = value
      End Set
   End Property

   Protected Overrides Sub AddMemberDataXMLToXMLWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("Name", m_Name)
      m_CompatableQueries.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXMLReader(ByVal Reader As System.Xml.XmlReader)
      m_Name = Reader.GetAttribute("Name")
      Reader.Read()
      m_CompatableQueries.PopulateFromXMLReader(Reader)
   End Sub

End Class
