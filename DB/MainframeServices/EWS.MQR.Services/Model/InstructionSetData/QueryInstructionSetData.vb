Imports EWS.XML

Public Class QueryInstructionSetData : Inherits XMLBase

   Private m_Name As String
   Private m_Parameters As New QueryInstructionSetParameters
   Private m_CompatableParses As New ParseInstructionSetDatas

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

   Public Property CompatableParses() As ParseInstructionSetDatas
      Get
         Return m_CompatableParses
      End Get
      Set(ByVal value As ParseInstructionSetDatas)
         m_CompatableParses = value
      End Set
   End Property

   Public ReadOnly Property Parameters() As QueryInstructionSetParameters
      Get
         Return m_Parameters
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXMLToXMLWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("Name", m_Name)
      m_CompatableParses.AddXMLToWriter(Writer)
      m_Parameters.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXMLReader(ByVal Reader As System.Xml.XmlReader)
      m_Name = Reader.GetAttribute("Name")
      Reader.Read()
      m_CompatableParses.PopulateFromXMLReader(Reader)
      m_Parameters.PopulateFromXMLReader(Reader)
   End Sub

End Class
