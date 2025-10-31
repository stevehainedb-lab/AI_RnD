Imports EWS.XML

<Serializable()> _
Public Class QueryRequestParameters : Inherits XMLBaseDictionary(Of QueryRequestParameter)

   Public Sub New()
      MyBase.New(MultiplicityKind.NoneToMany)
   End Sub

   Public Overrides Function NewItem() As QueryRequestParameter
      Return New QueryRequestParameter
   End Function

   Public Function UniqueString() As String
      Dim result As String = ""
      For Each Param As QueryRequestParameter In Me.Values
         If Not Param.Identifier.ToUpper = "asat".ToUpper And Not Param.Identifier.ToUpper = "CacheTTLSecs".ToUpper Then
            If Not Param.Value.Trim = String.Empty Then
               result += Param.Value.Trim
            End If
         End If
      Next
      Return result
   End Function

   Public Function SearchKey() As String

      Select Case True
         Case Me.ContainsKey("CalledTrainID")
            Return Me("CalledTrainID").Value
         Case Me.ContainsKey("CalledTrainID".ToUpper)
            Return Me("CalledTrainID".ToUpper).Value
         Case Me.ContainsKey("CalledTrainID".ToLower)
            Return Me("CalledTrainID".ToLower).Value

         Case Me.ContainsKey("CalledTrainIdentity")
            Return Me("CalledTrainIdentity").Value
         Case Me.ContainsKey("CalledTrainIdentity".ToUpper)
            Return Me("CalledTrainIdentity".ToUpper).Value
         Case Me.ContainsKey("CalledTrainIdentity".ToLower)
            Return Me("CalledTrainIdentity".ToLower).Value

         Case Me.ContainsKey("CTI")
            Return Me("CTI").Value
         Case Me.ContainsKey("CTI".ToUpper)
            Return Me("CTI".ToUpper).Value
         Case Me.ContainsKey("CTI".ToLower)
            Return Me("CTI".ToLower).Value

         Case Me.ContainsKey("TrainID")
            Return Me("TrainID").Value
         Case Me.ContainsKey("TrainID".ToUpper)
            Return Me("TrainID".ToUpper).Value
         Case Me.ContainsKey("TrainID".ToLower)
            Return Me("TrainID".ToLower).Value

         Case Me.ContainsKey("TrainIdentity")
            Return Me("TrainIdentity").Value
         Case Me.ContainsKey("TrainIdentity".ToUpper)
            Return Me("TrainIdentity".ToUpper).Value
         Case Me.ContainsKey("TrainIdentity".ToLower)
            Return Me("TrainIdentity".ToLower).Value

         Case Me.ContainsKey("TI")
            Return Me("TI").Value
         Case Me.ContainsKey("TI".ToUpper)
            Return Me("TI".ToUpper).Value
         Case Me.ContainsKey("TI".ToLower)
            Return Me("TI".ToLower).Value

         Case Me.ContainsKey("IncidentNumber")
            Return Me("IncidentNumber").Value
         Case Me.ContainsKey("IncidentNumber".ToUpper)
            Return Me("IncidentNumber".ToUpper).Value
         Case Me.ContainsKey("IncidentNumber".ToLower)
            Return Me("IncidentNumber".ToLower).Value

         Case Me.ContainsKey("F4Input")
            Return Me("F4Input").Value.Replace(" ", "")
         Case Me.ContainsKey("F4Input".ToUpper)
            Return Me("F4Input".ToUpper).Value.Replace(" ", "")
         Case Me.ContainsKey("F4Input".ToLower)
            Return Me("F4Input".ToLower).Value.Replace(" ", "")

         Case Me.ContainsKey("HeadCode")
            Return Me("HeadCode").Value
         Case Me.ContainsKey("HeadCode".ToUpper)
            Return Me("HeadCode".ToUpper).Value
         Case Me.ContainsKey("HeadCode".ToLower)
            Return Me("HeadCode".ToLower).Value

         Case Else
            Return ""
      End Select

   End Function

   Public Overrides Function ToString() As String
      Dim Result As String = ""
      For Each Param As QueryRequestParameter In Me.Values
         If Not Param.Identifier.ToUpper = "asat".ToUpper And Not Param.Identifier.ToUpper = "CacheTTLSecs".ToUpper Then
            If Not Param.Value.Trim = String.Empty Then
               Result += Param.Identifier & "-" & Param.Value.Trim & "_"
            End If
         End If
      Next

      If Result.Length > 0 Then
         Result = Result.Substring(0, Result.Length - 1)
      End If

      Return Result

   End Function

   Public ReadOnly Property AsAt() As Date
      Get
         Select Case True
            Case Me.ContainsKey("AsAt")
               Return CDate(Me("AsAt").Value)
            Case Me.ContainsKey("ASAT")
               Return CDate(Me("ASAT").Value)
            Case Me.ContainsKey("asat")
               Return CDate(Me("asat").Value)
            Case Else
               Return Now
         End Select
      End Get
   End Property

   Public ReadOnly Property CommandID() As String
      Get
         Select Case True
            Case Me.ContainsKey("CommandId")
               Return Me("CommandId").Value
            Case Me.ContainsKey("CommandID")
               Return Me("CommandID").Value
            Case Me.ContainsKey("ASAT")
               Return Me("COMMANDID").Value
            Case Me.ContainsKey("asat")
               Return Me("commandid").Value
            Case Else
               Return String.Empty
         End Select
      End Get
   End Property

   Public ReadOnly Property CacheTTLSecs As Integer
      Get
         Dim value As String
         Select Case True
            Case ContainsKey("CacheTTL")
               value = Me("CacheTTL").Value
            Case ContainsKey("CacheTtl")
               value = Me("CacheTtl").Value
            Case ContainsKey("CACHETTL")
               value = Me("CACHETTL").Value
            Case ContainsKey("cachettl")
               value = Me("cachettl").Value
            Case ContainsKey("CacheTTLSecs")
               value = Me("CacheTTLSecs").Value
            Case ContainsKey("CacheTtlSecs")
               value = Me("CacheTtlSecs").Value
            Case ContainsKey("CACHETTLSECS")
               value = Me("CACHETTLSECS").Value
            Case ContainsKey("cachettlsecs")
               value = Me("cachettlsecs").Value
            Case Else
               value = "0"
         End Select

         Dim intValue As Integer
         If Integer.TryParse(value, intValue) Then
            Return intValue
         Else
            Return 0
         End If

      End Get
   End Property

   Public Overloads Sub Add(ByVal item As QueryRequestParameter)
      MyBase.Add(item.Key, item)
   End Sub

End Class
