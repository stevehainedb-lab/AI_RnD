Imports EWS.XML
Imports EWS.XML.XMLUtilities


<Serializable()> _
Public Class QueryResult : Inherits XMLBase

   Private m_Sections As New QueryResultSections
   Private m_MFReponseTime As Date = Date.MinValue

   Public Sub New()

   End Sub

   Public Sub New(ByVal MFReponseTime As Date)
      m_MFReponseTime = MFReponseTime
   End Sub

   Public ReadOnly Property Sections() As QueryResultSections
      Get
         Return m_Sections
      End Get
   End Property

   Public ReadOnly Property MFReponseTime() As Date
      Get
         Return m_MFReponseTime
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("MFReponseTime", MFReponseTime.ToString("yyyy-MM-ddTHH:mm:ssz"))
      m_Sections.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)

      m_MFReponseTime = CDate(Reader.GetAttribute("MFReponseTime"))
      Reader.Read()
      m_Sections.PopulateFromXMLReader(Reader)
   End Sub

   Public Overrides Function ToString() As String
      Dim Records As Integer
      For Each Section As QueryResultSection In Sections.Values
         Records += Section.Rows.Count
      Next
      Return String.Format("Response has {0} sections which contain {1} Records.", Sections.Count, Records.ToString)
   End Function

   Public Sub Merge(ByVal Addition As QueryResult)
      Merge(Me, Addition)
   End Sub

   Public Shared Sub Merge(ByVal Base As QueryResult, ByVal Addition As QueryResult)
      For Each Section As QueryResultSection In Addition.Sections.Values
         If Base.Sections.ContainsKey(Section.Key) Then
            For Each Row As QueryResultRow In Section.Rows.Values
               If DirectCast(Base.Sections(Section.Key), QueryResultSection).Rows.ContainsKey(Row.Key) Then
                  For Each Field As QueryResultField In Row.Fields.Values
                     If DirectCast(DirectCast(Base.Sections(Section.Key), QueryResultSection).Rows(Row.Key), QueryResultRow).Fields.ContainsKey(Field.Key) Then
                        DirectCast(DirectCast(DirectCast(Base.Sections(Section.Key), QueryResultSection).Rows(Row.Key), QueryResultRow).Fields(Field.Key), QueryResultField).Value = Field.Value
                     Else
                        DirectCast(DirectCast(Base.Sections(Section.Key), QueryResultSection).Rows(Row.Key), QueryResultRow).Fields.Add(Field)
                     End If
                  Next
               Else
                  DirectCast(Base.Sections(Section.Key), QueryResultSection).Rows.Add(Row)
               End If
            Next
         Else
            Base.Sections.Add(Section)
         End If
      Next
   End Sub
End Class
