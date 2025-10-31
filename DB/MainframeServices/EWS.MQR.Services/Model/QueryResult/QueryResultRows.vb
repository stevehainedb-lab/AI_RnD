Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryResultRows : Inherits XMLBaseDictionary(Of QueryResultRow)

   Public Sub New()
      MyBase.New(MultiplicityKind.NoneToMany)
   End Sub

   Public Overrides Function NewItem() As QueryResultRow
      Return New QueryResultRow
   End Function

   Public Overloads Sub Add(ByVal item As QueryResultRow)
      MyBase.Add(item.Key, item)
   End Sub

   Public Function FirstRow() As QueryResultRow
      If Count = 0 Then
         Return Nothing
      Else
         Dim result As QueryResultRow = Nothing
         For Each Key As String In Me.Keys
            result = Item(Key)
            Exit For
         Next
         Return result
      End If
   End Function

End Class
