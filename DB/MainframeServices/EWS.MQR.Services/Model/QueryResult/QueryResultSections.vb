Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryResultSections : Inherits XMLBaseDictionary(Of QueryResultSection)

   Public Sub New()
      MyBase.New(MultiplicityKind.NoneToMany)
   End Sub

   Public Overrides Function NewItem() As QueryResultSection
      Return New QueryResultSection
   End Function

   Public Overloads Sub Add(ByVal item As QueryResultSection)
      MyBase.Add(item.Key, item)
   End Sub
End Class
