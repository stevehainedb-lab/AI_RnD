Imports System.Text.RegularExpressions
Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryResultFields : Inherits XMLBaseDictionary(Of QueryResultField)

   Public Sub New()
      MyBase.New(MultiplicityKind.NoneToMany)
   End Sub

   Public Overrides Function NewItem() As QueryResultField
      Return New QueryResultField
   End Function

   Public Overloads Sub Add(ByVal item As QueryResultField)
      MyBase.Add(item.Key, item)
   End Sub


End Class
